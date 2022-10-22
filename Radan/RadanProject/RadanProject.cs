using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.Collections;

namespace RadProject
{
    #region Project
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "2.0.50727.3038")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.radan.com/ns/project")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "http://www.radan.com/ns/project", IsNullable = false)]
    public partial class RadanProject
    {
        public string ProjectNestNameTemplate { get; set; }
        public string JobName { get; set; }
        public long FirstNestNumber { get; set; }
        public string RemnantUseFolder { get; set; }
        public string MatchMat { get; set; }
        public RadanSheets Sheets { get; set; }
        public RadanParts Parts { get; set; }
        public RadanProjectRemnantSheets RemnantSheets { get; set; }
        private RadanSchedule [] RadanScheduleField;
        private List<Remnant> remnantsField;
        private List<RadanNest> nestsField;



        [System.Xml.Serialization.XmlArrayItemAttribute("Remnant", typeof(Remnant), IsNullable = false)]
        public List<Remnant> Remnants
        {
            get
            {
                return this.remnantsField;
            }
            set
            {
                this.remnantsField = value;
            }
        }

        [System.Xml.Serialization.XmlArrayItemAttribute("Nest", typeof(RadanNest), IsNullable = false)]
        public List<RadanNest> Nests
        {
            get
            {
                return this.nestsField;
            }
            set
            {
                this.nestsField = value;
            }
        }

        [System.Xml.Serialization.XmlElementAttribute("RadanSchedule")]
        public RadanSchedule [] RadanSchedule
        {
            get
            {
                return this.RadanScheduleField;
            }
            set
            {
                this.RadanScheduleField = value;
            }
        }

        /*
        public Boolean RemoveNest(RadanProjectNestsNest nestToRemove)
        {
            try
            {
                this.Nests.Remove(nestToRemove);
                return true;
            }
            catch (Exception)
            {
                return false;
            }

        }
        
        // constructor to initialize new object
        public RadanProject()
        {
            ProjectNestNameTemplate = "?";
            JobName = "?";
            FirstNestNumber = 0;
            RemnantUseFolder = "";
            MatchMat = "";
            Remnants = "";
            //RadanSchedule = new MainRadanProjectRadanSchedule[1];
            Parts = new List<RadanProjectParts>();
            Nests = new List<RadanProjectNestsNest>();
        }

        // copy constructor
        public RadanProject(RadanProject prj)
        {
            this.FirstNestNumber = prj.FirstNestNumber;
            this.JobName = prj.JobName;
            this.MatchMat = prj.MatchMat;
            this.Nests = prj.Nests;
            this.Parts = prj.Parts;
            this.ProjectNestNameTemplate = prj.ProjectNestNameTemplate;
            this.RadanSchedule = prj.RadanSchedule;
            this.Remnants = prj.Remnants;
            this.RemnantSheets = prj.RemnantSheets;
            this.RemnantUseFolder = prj.RemnantUseFolder;
            this.Sheets = prj.Sheets;
        }
        */
    }

    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "2.0.50727.3038")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.radan.com/ns/project")]
    public partial class RadanSchedule
    {
        public string parts { get; set; }
        private JobDetails [] JobDetailsField;

        [System.Xml.Serialization.XmlElementAttribute("JobDetails")]
        public JobDetails []JobDetails
        {
            get
            {
                return this.JobDetailsField;
            }
            set
            {
                this.JobDetailsField = value;
            }
        }
    }

    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "2.0.50727.3038")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.radan.com/ns/project")]
    public partial class JobDetails
    {
        private string unitsField;
        private string jobNameField;
        private long nextNestNumField;
        private string annotateField;
        private string plotField;
        private string graphicsField;
        private string doShapedField;
        private string matchMatField;
        private string useExtraField;
        private string batchedField;
        private string maxTimeField;
        private string maxTimeBatchedField;
        private string progRedField;
        private string progRedUtilField;
        private string placementPrefField;
        private string primaryDirField;
        private string secondaryDirField;
        private string utiliseSheetSpacesField;
        private string nestInHolesField;
        private string checkToolingField;
        private string toolHintsField;
        private string stdToolIdField;
        private string restartResultsField;
        private string numNozzlesField;
        private string numNozzlesPerSheetField;
        private string mCSpecificFilesField;
        private string autoUpdateField;
        private string aUNumNestsField;
        private string aUNumSheetsField;
        private string simplificationField;
        private string ignorePensField;
        private string readMDBClearancesField;
        private string partGapXField;
        private string partGapYField;
        private string partGapXYField;
        private string minDatumVertField;
        private string minNondatumVertField;
        private string optVertField;
        private string clampStripFlagField;
        private string clampZoneFlagField;
        private string clampStripWidthField;
        private string clampSizeParallelField;
        private string clampSizePerpendicularField;
        private string minUnclampedField;
        private string optUnclampedField;
        private string minClampedField;
        private string sheetDrgBorderField;
        private string pickingClusterBorderField;
        private string pickingClusterGapXYField;
        private string pickingClusterMaxAreaField;
        private string pickingClusterPenField;
        private string commonCutGapXYField;
        private string commonCutGapAutoField;
        private string includeToolingInShapeField;
        private string autoToolField;
        private string sheetCutField;
        private string runPreOrderMacroField;
        private string preOrderMacroNameField;
        private string runPreAnnotateMacroField;
        private string preAnnotateMacroNameField;
        private string autoOrderField;
        private string autoOrderFileField;
        private string runNestMacroField;
        private string nestMacroNameField;
        private string autoCompileField;
        private string lastJobUnloadedField;
        private string useDefaultStrategyField;
        private string strategyField;
        private string strategySheetDwgField;
        private string strategyMultisheetField;
        private string nestFolderField;
        private string useRemnantsField;
        private string remnantUseFolderField;
        private string remnantPremiumField;
        private string remnantPriorityField;
        private string saveRemnantsField;
        private string remnantAreaWorthSavingField;
        private string remnantSaveFolderField;
        private string saveRectangularRemnantsOnlyField;
        private string alignRectanglesWithClampStripField;
        private string replaceOrigNestField;
        private string useProjectPartsField;
        private string useProjectSheetsField;
        private List<JobDetailsSheetSource> sheetSourceField;

        /// <remarks/>
        public string Units
        {
            get
            {
                return this.unitsField;
            }
            set
            {
                this.unitsField = value;
            }
        }

        /// <remarks/>
        public string JobName
        {
            get
            {
                return this.jobNameField;
            }
            set
            {
                this.jobNameField = value;
            }
        }

        /// <remarks/>
        public long NextNestNum
        {
            get
            {
                return this.nextNestNumField;
            }
            set
            {
                this.nextNestNumField = value;
            }
        }

        /// <remarks/>
        public string Annotate
        {
            get
            {
                return this.annotateField;
            }
            set
            {
                this.annotateField = value;
            }
        }

        /// <remarks/>
        public string Plot
        {
            get
            {
                return this.plotField;
            }
            set
            {
                this.plotField = value;
            }
        }

        /// <remarks/>
        public string Graphics
        {
            get
            {
                return this.graphicsField;
            }
            set
            {
                this.graphicsField = value;
            }
        }

        /// <remarks/>
        public string DoShaped
        {
            get
            {
                return this.doShapedField;
            }
            set
            {
                this.doShapedField = value;
            }
        }

        /// <remarks/>
        public string MatchMat
        {
            get
            {
                return this.matchMatField;
            }
            set
            {
                this.matchMatField = value;
            }
        }

        /// <remarks/>
        public string UseExtra
        {
            get
            {
                return this.useExtraField;
            }
            set
            {
                this.useExtraField = value;
            }
        }

        /// <remarks/>
        public string Batched
        {
            get
            {
                return this.batchedField;
            }
            set
            {
                this.batchedField = value;
            }
        }

        /// <remarks/>
        public string MaxTime
        {
            get
            {
                return this.maxTimeField;
            }
            set
            {
                this.maxTimeField = value;
            }
        }

        /// <remarks/>
        public string MaxTimeBatched
        {
            get
            {
                return this.maxTimeBatchedField;
            }
            set
            {
                this.maxTimeBatchedField = value;
            }
        }

        /// <remarks/>
        public string ProgRed
        {
            get
            {
                return this.progRedField;
            }
            set
            {
                this.progRedField = value;
            }
        }

        /// <remarks/>
        public string ProgRedUtil
        {
            get
            {
                return this.progRedUtilField;
            }
            set
            {
                this.progRedUtilField = value;
            }
        }

        /// <remarks/>
        public string PlacementPref
        {
            get
            {
                return this.placementPrefField;
            }
            set
            {
                this.placementPrefField = value;
            }
        }

        /// <remarks/>
        public string PrimaryDir
        {
            get
            {
                return this.primaryDirField;
            }
            set
            {
                this.primaryDirField = value;
            }
        }

        /// <remarks/>
        public string SecondaryDir
        {
            get
            {
                return this.secondaryDirField;
            }
            set
            {
                this.secondaryDirField = value;
            }
        }

        /// <remarks/>
        public string UtiliseSheetSpaces
        {
            get
            {
                return this.utiliseSheetSpacesField;
            }
            set
            {
                this.utiliseSheetSpacesField = value;
            }
        }

        /// <remarks/>
        public string NestInHoles
        {
            get
            {
                return this.nestInHolesField;
            }
            set
            {
                this.nestInHolesField = value;
            }
        }

        /// <remarks/>
        public string CheckTooling
        {
            get
            {
                return this.checkToolingField;
            }
            set
            {
                this.checkToolingField = value;
            }
        }

        /// <remarks/>
        public string ToolHints
        {
            get
            {
                return this.toolHintsField;
            }
            set
            {
                this.toolHintsField = value;
            }
        }

        /// <remarks/>
        public string StdToolId
        {
            get
            {
                return this.stdToolIdField;
            }
            set
            {
                this.stdToolIdField = value;
            }
        }

        /// <remarks/>
        public string RestartResults
        {
            get
            {
                return this.restartResultsField;
            }
            set
            {
                this.restartResultsField = value;
            }
        }

        /// <remarks/>
        public string NumNozzles
        {
            get
            {
                return this.numNozzlesField;
            }
            set
            {
                this.numNozzlesField = value;
            }
        }

        /// <remarks/>
        public string NumNozzlesPerSheet
        {
            get
            {
                return this.numNozzlesPerSheetField;
            }
            set
            {
                this.numNozzlesPerSheetField = value;
            }
        }

        /// <remarks/>
        public string MCSpecificFiles
        {
            get
            {
                return this.mCSpecificFilesField;
            }
            set
            {
                this.mCSpecificFilesField = value;
            }
        }

        /// <remarks/>
        public string AutoUpdate
        {
            get
            {
                return this.autoUpdateField;
            }
            set
            {
                this.autoUpdateField = value;
            }
        }

        /// <remarks/>
        public string AUNumNests
        {
            get
            {
                return this.aUNumNestsField;
            }
            set
            {
                this.aUNumNestsField = value;
            }
        }

        /// <remarks/>
        public string AUNumSheets
        {
            get
            {
                return this.aUNumSheetsField;
            }
            set
            {
                this.aUNumSheetsField = value;
            }
        }

        /// <remarks/>
        public string Simplification
        {
            get
            {
                return this.simplificationField;
            }
            set
            {
                this.simplificationField = value;
            }
        }

        /// <remarks/>
        public string IgnorePens
        {
            get
            {
                return this.ignorePensField;
            }
            set
            {
                this.ignorePensField = value;
            }
        }

        /// <remarks/>
        public string ReadMDBClearances
        {
            get
            {
                return this.readMDBClearancesField;
            }
            set
            {
                this.readMDBClearancesField = value;
            }
        }

        /// <remarks/>
        public string PartGapX
        {
            get
            {
                return this.partGapXField;
            }
            set
            {
                this.partGapXField = value;
            }
        }

        /// <remarks/>
        public string PartGapY
        {
            get
            {
                return this.partGapYField;
            }
            set
            {
                this.partGapYField = value;
            }
        }

        /// <remarks/>
        public string PartGapXY
        {
            get
            {
                return this.partGapXYField;
            }
            set
            {
                this.partGapXYField = value;
            }
        }

        /// <remarks/>
        public string MinDatumVert
        {
            get
            {
                return this.minDatumVertField;
            }
            set
            {
                this.minDatumVertField = value;
            }
        }

        /// <remarks/>
        public string MinNondatumVert
        {
            get
            {
                return this.minNondatumVertField;
            }
            set
            {
                this.minNondatumVertField = value;
            }
        }

        /// <remarks/>
        public string OptVert
        {
            get
            {
                return this.optVertField;
            }
            set
            {
                this.optVertField = value;
            }
        }

        /// <remarks/>
        public string ClampStripFlag
        {
            get
            {
                return this.clampStripFlagField;
            }
            set
            {
                this.clampStripFlagField = value;
            }
        }

        /// <remarks/>
        public string ClampZoneFlag
        {
            get
            {
                return this.clampZoneFlagField;
            }
            set
            {
                this.clampZoneFlagField = value;
            }
        }

        /// <remarks/>
        public string ClampStripWidth
        {
            get
            {
                return this.clampStripWidthField;
            }
            set
            {
                this.clampStripWidthField = value;
            }
        }

        /// <remarks/>
        public string ClampSizeParallel
        {
            get
            {
                return this.clampSizeParallelField;
            }
            set
            {
                this.clampSizeParallelField = value;
            }
        }

        /// <remarks/>
        public string ClampSizePerpendicular
        {
            get
            {
                return this.clampSizePerpendicularField;
            }
            set
            {
                this.clampSizePerpendicularField = value;
            }
        }

        /// <remarks/>
        public string MinUnclamped
        {
            get
            {
                return this.minUnclampedField;
            }
            set
            {
                this.minUnclampedField = value;
            }
        }

        /// <remarks/>
        public string OptUnclamped
        {
            get
            {
                return this.optUnclampedField;
            }
            set
            {
                this.optUnclampedField = value;
            }
        }

        /// <remarks/>
        public string MinClamped
        {
            get
            {
                return this.minClampedField;
            }
            set
            {
                this.minClampedField = value;
            }
        }

        /// <remarks/>
        public string SheetDrgBorder
        {
            get
            {
                return this.sheetDrgBorderField;
            }
            set
            {
                this.sheetDrgBorderField = value;
            }
        }

        /// <remarks/>
        public string PickingClusterBorder
        {
            get
            {
                return this.pickingClusterBorderField;
            }
            set
            {
                this.pickingClusterBorderField = value;
            }
        }

        /// <remarks/>
        public string PickingClusterGapXY
        {
            get
            {
                return this.pickingClusterGapXYField;
            }
            set
            {
                this.pickingClusterGapXYField = value;
            }
        }

        /// <remarks/>
        public string PickingClusterMaxArea
        {
            get
            {
                return this.pickingClusterMaxAreaField;
            }
            set
            {
                this.pickingClusterMaxAreaField = value;
            }
        }

        /// <remarks/>
        public string PickingClusterPen
        {
            get
            {
                return this.pickingClusterPenField;
            }
            set
            {
                this.pickingClusterPenField = value;
            }
        }

        /// <remarks/>
        public string CommonCutGapXY
        {
            get
            {
                return this.commonCutGapXYField;
            }
            set
            {
                this.commonCutGapXYField = value;
            }
        }

        /// <remarks/>
        public string CommonCutGapAuto
        {
            get
            {
                return this.commonCutGapAutoField;
            }
            set
            {
                this.commonCutGapAutoField = value;
            }
        }

        /// <remarks/>
        public string IncludeToolingInShape
        {
            get
            {
                return this.includeToolingInShapeField;
            }
            set
            {
                this.includeToolingInShapeField = value;
            }
        }

        /// <remarks/>
        public string AutoTool
        {
            get
            {
                return this.autoToolField;
            }
            set
            {
                this.autoToolField = value;
            }
        }

        /// <remarks/>
        public string SheetCut
        {
            get
            {
                return this.sheetCutField;
            }
            set
            {
                this.sheetCutField = value;
            }
        }

        /// <remarks/>
        public string RunPreOrderMacro
        {
            get
            {
                return this.runPreOrderMacroField;
            }
            set
            {
                this.runPreOrderMacroField = value;
            }
        }

        /// <remarks/>
        public string PreOrderMacroName
        {
            get
            {
                return this.preOrderMacroNameField;
            }
            set
            {
                this.preOrderMacroNameField = value;
            }
        }

        /// <remarks/>
        public string RunPreAnnotateMacro
        {
            get
            {
                return this.runPreAnnotateMacroField;
            }
            set
            {
                this.runPreAnnotateMacroField = value;
            }
        }

        /// <remarks/>
        public string PreAnnotateMacroName
        {
            get
            {
                return this.preAnnotateMacroNameField;
            }
            set
            {
                this.preAnnotateMacroNameField = value;
            }
        }

        /// <remarks/>
        public string AutoOrder
        {
            get
            {
                return this.autoOrderField;
            }
            set
            {
                this.autoOrderField = value;
            }
        }

        /// <remarks/>
        public string AutoOrderFile
        {
            get
            {
                return this.autoOrderFileField;
            }
            set
            {
                this.autoOrderFileField = value;
            }
        }

        /// <remarks/>
        public string RunNestMacro
        {
            get
            {
                return this.runNestMacroField;
            }
            set
            {
                this.runNestMacroField = value;
            }
        }

        /// <remarks/>
        public string NestMacroName
        {
            get
            {
                return this.nestMacroNameField;
            }
            set
            {
                this.nestMacroNameField = value;
            }
        }

        /// <remarks/>
        public string AutoCompile
        {
            get
            {
                return this.autoCompileField;
            }
            set
            {
                this.autoCompileField = value;
            }
        }

        /// <remarks/>
        public string LastJobUnloaded
        {
            get
            {
                return this.lastJobUnloadedField;
            }
            set
            {
                this.lastJobUnloadedField = value;
            }
        }

        /// <remarks/>
        public string UseDefaultStrategy
        {
            get
            {
                return this.useDefaultStrategyField;
            }
            set
            {
                this.useDefaultStrategyField = value;
            }
        }

        /// <remarks/>
        public string Strategy
        {
            get
            {
                return this.strategyField;
            }
            set
            {
                this.strategyField = value;
            }
        }

        /// <remarks/>
        public string StrategySheetDwg
        {
            get
            {
                return this.strategySheetDwgField;
            }
            set
            {
                this.strategySheetDwgField = value;
            }
        }

        /// <remarks/>
        public string StrategyMultisheet
        {
            get
            {
                return this.strategyMultisheetField;
            }
            set
            {
                this.strategyMultisheetField = value;
            }
        }

        /// <remarks/>
        public string NestFolder
        {
            get
            {
                return this.nestFolderField;
            }
            set
            {
                this.nestFolderField = value;
            }
        }

        /// <remarks/>
        public string UseRemnants
        {
            get
            {
                return this.useRemnantsField;
            }
            set
            {
                this.useRemnantsField = value;
            }
        }

        /// <remarks/>
        public string RemnantUseFolder
        {
            get
            {
                return this.remnantUseFolderField;
            }
            set
            {
                this.remnantUseFolderField = value;
            }
        }

        /// <remarks/>
        public string RemnantPremium
        {
            get
            {
                return this.remnantPremiumField;
            }
            set
            {
                this.remnantPremiumField = value;
            }
        }

        /// <remarks/>
        public string RemnantPriority
        {
            get
            {
                return this.remnantPriorityField;
            }
            set
            {
                this.remnantPriorityField = value;
            }
        }

        /// <remarks/>
        public string SaveRemnants
        {
            get
            {
                return this.saveRemnantsField;
            }
            set
            {
                this.saveRemnantsField = value;
            }
        }

        /// <remarks/>
        public string RemnantAreaWorthSaving
        {
            get
            {
                return this.remnantAreaWorthSavingField;
            }
            set
            {
                this.remnantAreaWorthSavingField = value;
            }
        }

        /// <remarks/>
        public string RemnantSaveFolder
        {
            get
            {
                return this.remnantSaveFolderField;
            }
            set
            {
                this.remnantSaveFolderField = value;
            }
        }

        /// <remarks/>
        public string SaveRectangularRemnantsOnly
        {
            get
            {
                return this.saveRectangularRemnantsOnlyField;
            }
            set
            {
                this.saveRectangularRemnantsOnlyField = value;
            }
        }

        /// <remarks/>
        public string AlignRectanglesWithClampStrip
        {
            get
            {
                return this.alignRectanglesWithClampStripField;
            }
            set
            {
                this.alignRectanglesWithClampStripField = value;
            }
        }

        /// <remarks/>
        public string ReplaceOrigNest
        {
            get
            {
                return this.replaceOrigNestField;
            }
            set
            {
                this.replaceOrigNestField = value;
            }
        }

        /// <remarks/>
        public string UseProjectParts
        {
            get
            {
                return this.useProjectPartsField;
            }
            set
            {
                this.useProjectPartsField = value;
            }
        }

        /// <remarks/>
        public string UseProjectSheets
        {
            get
            {
                return this.useProjectSheetsField;
            }
            set
            {
                this.useProjectSheetsField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("SheetSource")]
        public List<JobDetailsSheetSource> SheetSource
        {
            get
            {
                return this.sheetSourceField;
            }
            set
            {
                this.sheetSourceField = value;
            }
        }

    }

    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "2.0.50727.3038")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.radan.com/ns/project")]
    public partial class JobDetailsSheetSource
    {
        private List<RadanSheet> sheetField;
        private string sourceField;

        [System.Xml.Serialization.XmlElementAttribute("Sheet")]
        public List<RadanSheet> Sheet
        {
            get
            {
                return this.sheetField;
            }
            set
            {
                this.sheetField = value;
            }
        }

        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string source
        {
            get
            {
                return this.sourceField;
            }
            set
            {
                this.sourceField = value;
            }
        }
    }



    #endregion

    #region Sheets
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "2.0.50727.3038")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.radan.com/ns/project")]
    public partial class RadanSheets
    {
        public string NextID { get; set; }
        private List<RadanSheet> sheetField;

        [System.Xml.Serialization.XmlElementAttribute("Sheet")]
        public List<RadanSheet> Sheet
        {
            get
            {
                return this.sheetField;
            }
            set
            {
                this.sheetField = value;
            }
        }
        /*
        // copy constructor
        public RadanProjectSheets(RadanProjectSheets sheets)
        {
            this.NextID = sheets.NextID;
            foreach (Sheet sht in Sheet)
            {
                Sheet newSheet = new Sheet(sht);
                this.Sheet.Add(newSheet);
            }
        }
        */
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "2.0.50727.3038")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.radan.com/ns/project")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "http://www.radan.com/ns/project", IsNullable = false)]
    public partial class RadanSheet
    {
        public string Material { get; set; }
        public double Thickness { get; set; }
        public string ThickUnits { get; set; }
        public double SheetX { get; set; }
        public double SheetY { get; set; }
        public string SheetUnits { get; set; }
        public string StockID { get; set; }
        public long ID { get; set; }
        public int NumAvailable { get; set; }
        public string Exclude { get; set; }
        public int Priority { get; set; }
        public int Used { get; set; }
        private List<SheetUsedInNest> usedInNestsField;

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayItemAttribute("UsedInNest", typeof(SheetUsedInNest), IsNullable = false)]
        public List<SheetUsedInNest> UsedInNests
        {
            get
            {
                return this.usedInNestsField;
            }
            set
            {
                this.usedInNestsField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "2.0.50727.3038")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.radan.com/ns/project")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "http://www.radan.com/ns/project", IsNullable = false)]
    public partial class SheetsUsedInNests
    {
        private List<SheetUsedInNest> usedInNestField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("UsedInNest")]
        public List<SheetUsedInNest> UsedInNest
        {
            get
            {
                return this.usedInNestField;
            }
            set
            {
                this.usedInNestField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "2.0.50727.3038")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.radan.com/ns/project")]
    public partial class SheetUsedInNest
    {
        public long ID { get; set; }
        public int Made { get; set; }
        public int Used { get; set; }
    }

    #endregion

    #region RemnantSheets

    // this section needs to be fleshed out yet, i don't think the original project file this code was generated from 
    // had any remanants in it.

    // .......should be done now, but not yet tested....

    public partial class RadanProjectRemnantSheets
    {
        public string NextID { get; set; }
        private List<RadanProjectRemnantSheet> remnantSheetField;

        [System.Xml.Serialization.XmlElementAttribute("RemnantSheet")]
        public List<RadanProjectRemnantSheet> Sheet
        {
            get
            {
                return this.remnantSheetField;
            }
            set
            {
                this.remnantSheetField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "2.0.50727.3038")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.radan.com/ns/project")]
    public partial class RadanProjectRemnantSheet
    {
        public string FileName { get; set; }
        public long ID { get; set; }
        public int Used { get; set; }
        private List<SheetUsedInNest> usedInNestsField;

        [System.Xml.Serialization.XmlArrayItemAttribute("UsedInNest", typeof(SheetUsedInNest), IsNullable = false)]
        public List<SheetUsedInNest> UsedInNests
        {
            get
            {
                return this.usedInNestsField;
            }
            set
            {
                this.usedInNestsField = value;
            }
        }
    }

    #endregion

    #region Remnants
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "2.0.50727.3038")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.radan.com/ns/project")]
    public partial class RemnantMade
    {
        public long ID { get; set; }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "2.0.50727.3038")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.radan.com/ns/project")]
    public partial class Remnant
    {
        public string FileName { get; set; }
        public long ID { get; set; }
        private List<RemnantSourceNest> sourceNestField;

        [System.Xml.Serialization.XmlElementAttribute("SourceNest")]
        public List<RemnantSourceNest> SourceNest
        {
            get
            {
                return this.sourceNestField;
            }
            set
            {
                this.sourceNestField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "2.0.50727.3038")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.radan.com/ns/project")]
    public partial class RemnantSourceNest
    {
        public long ID { get; set; }
    }
    #endregion

    #region Parts
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "2.0.50727.3038")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.radan.com/ns/project")]
    public partial class RadanParts
    {
        public long NextID { get; set; }

        private List<RadanPart> partField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("Part")]
        public List<RadanPart> Part
        {
            get
            {
                return this.partField;
            }
            set
            {
                this.partField = value;
            }
        }

        public Boolean Add(RadanPart rPrt)
        {
            Part.Add(rPrt);
            return true;
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "2.0.50727.3038")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.radan.com/ns/project")]
    public partial class RadanPart
    {
        public long ID { get; set; }
        public string Symbol { get; set; }
        public string Kit { get; set; }
        public int Number { get; set; }
        public int NumExtra { get; set; }
        public int Priority { get; set; }
        public string Bin { get; set; }
        public int Orient { get; set; }
        public string Mirror { get; set; }
        public string CCut { get; set; }
        public int MaxCCut { get; set; }
        public string PickingCluster { get; set; }
        public string Material { get; set; }
        public double Thickness { get; set; }
        public string ThickUnits { get; set; }
        public string Strategy { get; set; }
        public string Exclude { get; set; }
        public int Made { get; set; }
        public string CustomColour { get; set; }
        public string ColourWhenPartSaved { get; set; }
        private List<SheetUsedInNest> usedInNestsField;

        [System.Xml.Serialization.XmlArrayItemAttribute("UsedInNest", typeof(SheetUsedInNest), IsNullable = false)]
        public List<SheetUsedInNest> UsedInNests
        {
            get
            {
                return this.usedInNestsField;
            }
            set
            {
                this.usedInNestsField = value;
            }
        }

        // default constructor needed because class is serializeable 
        // and because I added a constructor with parameters.
        public RadanPart()
        {
            this.Bin = "";
            this.CCut = "";
            this.Exclude = "";
            this.ID = 0;
            this.Kit = "";
            this.Made = 0;
            this.Material = "";
            this.MaxCCut = 2;
            this.Mirror = "";
            this.Number = 0;
            this.NumExtra = 0;
            this.Orient = 8;
            this.PickingCluster = "n";
            this.Priority = 5;
            this.Strategy = "";
            this.Symbol = "";
            this.Thickness = 0.0;
            this.ThickUnits = "";
            this.CustomColour = "255,255,255";
            this.ColourWhenPartSaved = "204, 204, 204";
            this.UsedInNests = new List<SheetUsedInNest>();
        }

        // constructor for adding new parts to the project
        public RadanPart(string symFile, long id, string mat, double thick, string thickUnits, int qty)
        {
            this.Bin = "";
            this.CCut = "none";
            this.Exclude = "n";
            this.ID = id;
            this.Kit = "";
            this.Made = 0;
            this.Material = mat;
            this.MaxCCut = 2;
            this.Mirror = "n";
            this.Number = qty;
            this.NumExtra = 0;
            this.Orient = 8;
            this.PickingCluster = "n";
            this.Priority = 5;
            this.Strategy = "";
            this.Symbol = symFile;
            this.Thickness = thick;
            this.ThickUnits = thickUnits;
            this.CustomColour = "255,255,255";
            this.ColourWhenPartSaved = "204, 204, 204";

            this.UsedInNests = new List<SheetUsedInNest>();
        }

        /*
        // copy constructor
        public RadanProjectPartsPart(RadanProjectPartsPart prt)
        {
            this.Bin = prt.Bin;
            this.CCut = prt.CCut;
            this.Exclude = prt.Exclude;
            this.ID = prt.ID;
            this.Kit = prt.Kit;
            this.Made = prt.Made;
            this.Material = prt.Material;
            this.MaxCCut = prt.MaxCCut;
            this.Mirror = prt.Mirror;
            this.Number = prt.Number;
            this.NumExtra = prt.NumExtra;
            this.Orient = prt.Orient;
            this.PickingCluster = prt.PickingCluster;
            this.Priority = prt.Priority;
            this.Strategy = prt.Strategy;
            this.Symbol = prt.Symbol;
            this.Thickness = prt.Thickness;
            this.ThickUnits = prt.ThickUnits;
            this.UsedInNests = new List<UsedInNestsUsedInNest>(prt.UsedInNests);
        }
        */
    }
    #endregion

    #region Nests
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "2.0.50727.3038")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.radan.com/ns/project")]
    public partial class RadanNest
    {
        public string FileName { get; set; }
        public string ID { get; set; }
        public string Modified { get; set; }
        public string MultiPartSymbols { get; set; }
        private List<NestSheetsUsed> sheetUsedField;
        private List<NestsPartsMade> partsMadeField;

        [System.Xml.Serialization.XmlElementAttribute("SheetUsed")]
        public List<NestSheetsUsed> SheetUsed
        {
            get
            {
                return this.sheetUsedField;
            }
            set
            {
                this.sheetUsedField = value;
            }
        }

        [System.Xml.Serialization.XmlArrayItemAttribute("PartMade", typeof(NestsPartsMade), IsNullable = false)]
        public List<NestsPartsMade> PartsMade
        {
            get
            {
                return this.partsMadeField;
            }
            set
            {
                this.partsMadeField = value;
            }
        }




    }

    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "2.0.50727.3038")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.radan.com/ns/project")]
    public partial class NestSheetsUsed
    {
        public int Used { get; set; }
        public string Material { get; set; }
        public double Thickness { get; set; }
        public string ThickUnits { get; set; }
        public double SheetX { get; set; }
        public double SheetY { get; set; }
        public string SheetUnits { get; set; }
        public string StockID { get; set; }
        private List<NestSheetsUsedListItem> sheetsListItemsField;

        [System.Xml.Serialization.XmlArrayItemAttribute("SheetsListItem", typeof(NestSheetsUsedListItem), IsNullable = false)]
        public List<NestSheetsUsedListItem> SheetsListItems
        {
            get
            {
                return this.sheetsListItemsField;
            }
            set
            {
                this.sheetsListItemsField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "2.0.50727.3038")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.radan.com/ns/project")]
    public partial class NestSheetsUsedListItem
    {
        public long ID { get; set; }
        public int Used { get; set; }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "2.0.50727.3038")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.radan.com/ns/project")]
    public partial class NestsPartsMade
    {
        public string File { get; set; }
        public int Made { get; set; }
        private List<NestPartsMadeListItem> partsListItemsField;

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayItemAttribute("PartsListItem", typeof(NestPartsMadeListItem), IsNullable = false)]
        public List<NestPartsMadeListItem> PartsListItems
        {
            get
            {
                return this.partsListItemsField;
            }
            set
            {
                this.partsListItemsField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "2.0.50727.3038")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.radan.com/ns/project")]
    public partial class NestPartsMadeListItem
    {
        public long ID { get; set; }
        public int Made { get; set; }
    }
    #endregion
}
