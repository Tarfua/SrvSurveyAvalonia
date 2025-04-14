﻿using Newtonsoft.Json;
using SrvSurvey.plotters;
using System.Diagnostics;

namespace SrvSurvey.game
{
    class ColonyData : Data
    {
        public static string SystemColonisationShip = "System Colonisation Ship";
        public static string ExtPanelColonisationShip = "$EXT_PANEL_ColonisationShip:#index=1;";

        public static bool isConstructionSite(Docked? entry)
        {
            return entry != null && entry.StationServices?.Contains("colonisationcontribution") == true;
        }

        public static string getDefaultProjectName(Docked lastDocked)
        {
            var defaultName = lastDocked.StationName == ColonyData.SystemColonisationShip || lastDocked.StationName == ColonyData.ExtPanelColonisationShip
                ? $"Primary port: {lastDocked.StarSystem}"
                : lastDocked.StationName
                    .Replace("Planetary Construction Site:", "")
                    .Replace("Orbital Construction Site:", "")
                    .Trim()
                ?? "";

            return defaultName;
        }

        public static ColonyData Load(string fid, string cmdr)
        {
            var filepath = Path.Combine(Program.dataFolder, $"{fid}-colony.json");

            var data = Data.Load<ColonyData>(filepath);
            if (data == null)
                data = new ColonyData() { filepath = filepath };

            // migrate from original file format
            data.cmdr ??= cmdr;

            return data;
        }

        public async Task fetchLatest(string? buildId = null)
        {
            var form = Program.getPlotter<PlotBuildCommodities>();

            // set an empty dictionary to make it render "updating..."
            if (form != null)
            {
                form.pendingDiff = new();
                form.Invalidate();
            }

            if (buildId == null)
            {
                // fetch all ACTIVE projects and primaryBuildId
                await Task.WhenAll(
                    Game.colony.getPrimary(this.cmdr).continueOnMain(null, newPrimaryBuildId => this.primaryBuildId = newPrimaryBuildId, true),
                    Game.colony.getCmdrActive(this.cmdr).continueOnMain(null, newProjects => this.projects = newProjects)
                );
                if (this.primaryBuildId != null && getProject(this.primaryBuildId) == null)
                {
                    Game.log($"Not found: primaryBuildId: ${primaryBuildId} ?");
                    this.primaryBuildId = null;
                    //Debugger.Break();
                }
            }
            else
            {
                // fetch just the given project
                var freshProject = await Game.colony.getProject(buildId);
                var idx = this.projects.FindIndex(p => p.buildId == buildId);
                if (idx >= 0)
                    this.projects[idx] = freshProject;
            }

            // extract all marketId's from linked FCs
            this.allLinkedFCs.Clear();
            this.projects.ForEach(p => p.linkedFC.ForEach(fc => this.allLinkedFCs[fc.marketId] = fc));

            this.prepNeeds();

            //Game.log(this.colonySummary?.buildIds.formatWithHeader($"loadAllBuildProjects: loading: {this.colonySummary?.buildIds.Count} ...", "\r\n\t"));

            Game.log(this.projects.Select(p => p.buildId).formatWithHeader($"colonySummary.buildIds: {this.projects.Count}", "\r\n\t"));
            this.Save();

            if (form != null)
            {
                form.pendingDiff = null;
                form.Invalidate();
            }
        }

        #region instance data

        public List<Project> projects = new();

        public string cmdr;
        public string? primaryBuildId;
        public bool fcTracking = true;
        public Dictionary<string, int> fcCommodities = new();

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, DefaultValueHandling = DefaultValueHandling.Ignore)]
        public Dictionary<long, ProjectFC> allLinkedFCs = new();

        #endregion

        [JsonIgnore]
        public Needs allNeeds;

        public void prepNeeds()
        {
            allNeeds = getLocalNeeds(-1, -1);
            Game.log("ColonyData.prepNeeds: done");
        }

        public Project? getProject(string buildId)
        {
            if (buildId == null) return null;
            return projects?.FirstOrDefault(p => p.buildId == buildId);
        }

        public Project? getProject(long id64, long marketId)
        {
            return projects?.FirstOrDefault(p => p.systemAddress == id64 && p.marketId == marketId);
        }

        public string? getBuildId(Docked? entry)
        {
            if (entry == null)
                return null;
            else
                return projects?.FirstOrDefault(p => p.systemAddress == entry.SystemAddress && p.marketId == entry.MarketID)?.buildId;
        }

        public string? getBuildId(long id64, long marketId)
        {
            return projects?.FirstOrDefault(p => p.systemAddress == id64 && p.marketId == marketId)?.buildId;
        }

        public bool has(Docked? docked)
        {
            if (docked == null)
                return false;
            else
                return has(docked.SystemAddress, docked.MarketID);
        }

        public bool has(long address, long marketId)
        {
            return projects.Any(p => p.systemAddress == address && p.marketId == marketId);
        }

        public Needs getLocalNeeds(long address, long marketId)
        {
            var needs = new Needs();

            foreach (var project in projects)
            {
                if (address != -1 && project.systemAddress != address) continue;
                if (marketId != -1 && project.marketId != marketId) continue;

                // prep aggregated commodity needs
                foreach (var (commodity, need) in project.commodities)
                {
                    needs.commodities.init(commodity);
                    needs.commodities[commodity] += need;
                }

                // prep assignments
                var assignment = project.commanders?.GetValueOrDefault(cmdr, StringComparison.OrdinalIgnoreCase);
                if (assignment != null)
                    foreach (var commodity in assignment)
                        needs.assigned.Add(commodity);
            }

            return needs;
        }

        public void contributeNeeds(long systemAddress, long marketId, Dictionary<string, int> diff)
        {
            var localProject = this.getProject(systemAddress, marketId);
            if (localProject == null)
            {
                Game.log(diff.formatWithHeader($"TODO! Supplying commodities for untracked project: {systemAddress}/{marketId}", "\r\n\t"));
                // TODO: call the API but do no tracking
                return;
            }
            else
            {
                Game.log(diff.formatWithHeader($"Supplying commodities for: {localProject.buildId} ({systemAddress}/{marketId})", "\r\n\t"));

                var form = Program.getPlotter<PlotBuildCommodities>();
                if (form != null)
                {
                    form.pendingDiff = diff;
                    form.Invalidate();
                }

                Game.colony.contribute(localProject.buildId, this.cmdr, diff).continueOnMain(null, () =>
                {
                    // wait a bit then force plotter to re-render
                    Task.Delay(500).ContinueWith(t =>
                    {
                        if (form != null && !form.IsDisposed)
                        {
                            form.pendingDiff = null;
                            form.Invalidate();
                        }
                    });
                });
            }
        }

        public void checkConstructionSiteUponDocking(Docked entry, SystemBody? body)
        {
            var proj = this.getProject(entry.SystemAddress, entry.MarketID);
            if (proj == null) return;

            ProjectUpdate? updatedProject = null;

            // update faction name?
            if (proj.factionName != entry.StationFaction.Name)
            {
                Game.log($"Auto-update factionName: {proj.factionName} => {entry.StationFaction.Name}");
                updatedProject ??= new(proj.buildId);
                updatedProject.factionName = entry.StationFaction.Name;
            }

            // update body name/num
            if (body != null && (proj.bodyNum != body.id || proj.bodyName != body.name))
            {
                Game.log($"Auto-update bodyName/bodyNum: {proj.bodyName} => {body.name} / {proj.bodyNum} => {body.id}");
                updatedProject ??= new(proj.buildId);
                updatedProject.bodyName = body.name;
                updatedProject.bodyNum = body.id;
            }

            if (updatedProject != null)
                Game.colony.update(updatedProject).justDoIt();
        }

        public void updateNeeds(ColonisationConstructionDepot entry, long id64)
        {
            var proj = this.getProject(id64, entry.MarketID);
            if (proj == null) return;

            var form = Program.getPlotter<PlotBuildCommodities>();
            if (form != null)
            {
                form.pendingDiff = new();
                form.Invalidate();
            }

            var needed = entry.ResourcesRequired.ToDictionary(
                r => r.Name.Substring(1).Replace("_name;", ""),
                r => r.RequiredAmount - r.ProvidedAmount
            );
            var totalDiff = needed.Keys
                .Select(k => needed[k] - proj.commodities.GetValueOrDefault(k, 0)).Sum();
            if (totalDiff != 0)
            {
                var foo = new ProjectUpdate(proj.buildId)
                {
                    commodities = needed,
                    maxNeed = entry.ResourcesRequired.Sum(r => r.RequiredAmount),
                };

                Game.colony.update(foo).continueOnMain(null, updatedProj =>
                {
                    // update in-memory track
                    var idx = this.projects.FindIndex(p => p.buildId == updatedProj.buildId);
                    this.projects[idx] = updatedProj;
                    this.Save();

                    Game.log(updatedProj);

                    if (form != null)
                    {
                        form.pendingDiff = null;
                        form.Invalidate();
                    }
                }).justDoIt();
            }
        }

        /* TODO: coming soon ...
        public Dictionary<string, int> sumProjectLinkedFC(Project proj)
        {
            var cargo = new Dictionary<string, int>();

            foreach(var fc in this.allLinkedFCs.Values)
            {
                // skip unrelated FCs
                if (proj.linkedFC.Any(_ => _.marketId == fc.marketId) == false) continue;

            }

            return cargo;
        }
        */

        public class Needs
        {
            public Dictionary<string, int> commodities = new();
            public HashSet<string> assigned = new();
        }

        public static Dictionary<string, string[]> mapCargoType = new Dictionary<string, string[]>()
        {
            { "Chemicals", new string[] { "liquidoxygen","pesticides","surfacestabilisers","water" } },
            { "Consumer Items", new string[] { "evacuationshelter","survivalequipment","beer","liquor","wine" } },
            { "Foods", new string[] { "animalmeat","coffee","fish","foodcartridges","fruitandvegetables","grain","tea" } },
            { "Industrial Materials", new string[] { "ceramiccomposites","cmmcomposite","insulatingmembrane","polymers","semiconductors","superconductors" } },
            { "Machinery", new string[] { "buildingfabricators","cropharvesters","emergencypowercells","geologicalequipment","microbialfurnaces","mineralextractors","powergenerators","thermalcoolingunits","waterpurifiers" } },
            { "Medicines", new string[] { "agriculturalmedicines","basicmedicines","combatstabilisers" } },
            { "Metals", new string[] { "aluminium","copper","steel","titanium" } },
            { "Technology", new string[] { "advancedcatalysers","bioreducinglichen","computercomponents","hazardousenvironmentsuits","landenrichmentsystems","medicaldiagnosticequipment","microcontrollers","muonimager","resonatingseparators","robotics","structuralregulators" } },
            { "Textiles", new string[] { "militarygradefabrics" } },
            { "Waste", new string[] { "biowaste" } },
            { "Weapons", new string[] { "battleweapons","nonlethalweapons","reactivearmour" } },
        };

        public static string getTypeForCargo(string name)
        {
            return mapCargoType.Keys.FirstOrDefault(key => mapCargoType[key].Contains(name))!;
        }
    }
}
