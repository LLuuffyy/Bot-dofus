class dofus.graphics.gapi.ui.StatsJob extends dofus.graphics.gapi.core.DofusAdvancedComponent
{
   var _avAlignment;
   var _btn10;
   var _btn11;
   var _btn12;
   var _btn13;
   var _btn14;
   var _btn15;
   var _btnClose;
   var _btnCloseJobs;
   var _btnClosePanel;
   var _btnJobOpen;
   var _btnResetStats;
   var _cbModulatedLevel;
   var _ctrAlignment;
   var _ctrJob0;
   var _ctrJob1;
   var _ctrJob2;
   var _ctrJob3;
   var _ctrJob4;
   var _ctrJob5;
   var _ctrJob6;
   var _ctrJob7;
   var _ctrJob8;
   var _ctrJob9;
   var _ctrJob10;
   var _ctrJob11;
   var _ctrJob12;
   var _ctrJob13;
   var _ctrJob14;
   var _ctrJob15;
   var _ctrJob16;
   var _ctrJob17;
   var _ctrJob18;
   var _ctrJob19;
   var _ctrJob20;
   var _ctrJob21;
   var _ctrJob22;
   var _ctrJob23;
   var _ctrSpe0;
   var _ctrSpe1;
   var _ctrSpe2;
   var _ctrSpe3;
   var _ctrSpe4;
   var _ctrSpe5;
   var _ctrSpe6;
   var _ctrSpe7;
   var _ctrSpe8;
   var _ctrSpe9;
   var _ctrSpe10;
   var _ctrSpe11;
   var _ctrSpe12;
   var _jvJob;
   var _lblAP;
   var _lblAPValue;
   var _lblAgility;
   var _lblAgilityValue;
   var _lblCapital;
   var _lblCapitalValue;
   var _lblChance;
   var _lblChanceValue;
   var _lblCharacteristics;
   var _lblDiscernment;
   var _lblDiscernmentValue;
   var _lblEnergy;
   var _lblForce;
   var _lblForceValue;
   var _lblInitiative;
   var _lblInitiativeValue;
   var _lblIntelligence;
   var _lblIntelligenceValue;
   var _lblLP;
   var _lblLPValue;
   var _lblLPmaxValue;
   var _lblLevel;
   var _lblMP;
   var _lblMPValue;
   var _lblMyJobs;
   var _lblName;
   var _lblSpecialization;
   var _lblVitality;
   var _lblVitalityValue;
   var _lblWisdom;
   var _lblWisdomValue;
   var _lblXP;
   var _mcAboutModulatedLevel;
   var _mcAboutRestats;
   var _mcAboutWisdom;
   var _mcEnergy;
   var _mcMoreStats;
   var _mcOverEnergy;
   var _mcViewersPlacer;
   var _mcViewersPlacer2;
   var _mcXP;
   var _oCurrentJob;
   var _parent;
   var _pbEnergy;
   var _pbXP;
   var _popupQuantity;
   var _svStats;
   var addToQueue;
   var attachMovie;
   var gapi;
   var getNextHighestDepth;
   var unloadThis;
   static var CLASS_NAME = "StatsJob";
   function StatsJob()
   {
      super();
   }
   function init()
   {
      super.init(false,dofus.graphics.gapi.ui.StatsJob.CLASS_NAME);
   }
   function destroy()
   {
      this.gapi.hideTooltip();
      if(this._popupQuantity != undefined)
      {
         this._popupQuantity.callClose();
      }
   }
   function callClose()
   {
      this.unloadThis();
      return true;
   }
   function createChildren()
   {
      this.showJobsPanel(false);
      if(this._btn12 != undefined)
      {
         this._btn12._visible = false;
         this._btn12.enabled = false;
      }
      this.addToQueue({object:this,method:this.addListeners});
      this.addToQueue({object:this,method:this.initData});
      this.addToQueue({object:this,method:this.initTexts});
      this.addToQueue({object:this,method:this.initTemporis});
      this._mcViewersPlacer._visible = false;
      this._btnClosePanel._visible = false;
      if(Key.isDown(Key.SHIFT))
      {
         this.showStats();
      }
      this.api.datacenter.Player.data.addListener(this);
      this.api.datacenter.Player.addEventListener("nameChanged",this);
      this.api.datacenter.Player.addEventListener("levelChanged",this);
      this.api.datacenter.Player.addEventListener("xpChanged",this);
      this.api.datacenter.Player.addEventListener("lpChanged",this);
      this.api.datacenter.Player.addEventListener("lpMaxChanged",this);
      this.api.datacenter.Player.addEventListener("apChanged",this);
      this.api.datacenter.Player.addEventListener("mpChanged",this);
      this.api.datacenter.Player.addEventListener("initiativeChanged",this);
      this.api.datacenter.Player.addEventListener("discernmentChanged",this);
      this.api.datacenter.Player.addEventListener("forceXtraChanged",this);
      this.api.datacenter.Player.addEventListener("vitalityXtraChanged",this);
      this.api.datacenter.Player.addEventListener("wisdomXtraChanged",this);
      this.api.datacenter.Player.addEventListener("chanceXtraChanged",this);
      this.api.datacenter.Player.addEventListener("agilityXtraChanged",this);
      this.api.datacenter.Player.addEventListener("intelligenceXtraChanged",this);
      this.api.datacenter.Player.addEventListener("bonusPointsChanged",this);
      this.api.datacenter.Player.addEventListener("energyChanged",this);
      this.api.datacenter.Player.addEventListener("energyMaxChanged",this);
      this.api.datacenter.Player.addEventListener("alignmentChanged",this);
   }
   function addListeners()
   {
      this.showJobsPanel(false);
      this._ctrAlignment.addEventListener("click",this);
      this._ctrJob0.addEventListener("click",this);
      this._ctrJob1.addEventListener("click",this);
      this._ctrJob2.addEventListener("click",this);
      this._ctrJob3.addEventListener("click",this);
      this._ctrJob4.addEventListener("click",this);
      this._ctrJob5.addEventListener("click",this);
      this._ctrJob6.addEventListener("click",this);
      this._ctrJob7.addEventListener("click",this);
      this._ctrJob8.addEventListener("click",this);
      this._ctrJob9.addEventListener("click",this);
      this._ctrJob10.addEventListener("click",this);
      this._ctrJob11.addEventListener("click",this);
      this._ctrJob12.addEventListener("click",this);
      this._ctrJob13.addEventListener("click",this);
      this._ctrJob14.addEventListener("click",this);
      this._ctrJob15.addEventListener("click",this);
      this._ctrJob16.addEventListener("click",this);
      this._ctrJob17.addEventListener("click",this);
      this._ctrJob18.addEventListener("click",this);
      this._ctrJob19.addEventListener("click",this);
      this._ctrJob20.addEventListener("click",this);
      this._ctrJob21.addEventListener("click",this);
      this._ctrJob22.addEventListener("click",this);
      this._ctrJob23.addEventListener("click",this);
      this._ctrSpe0.addEventListener("click",this);
      this._ctrSpe1.addEventListener("click",this);
      this._ctrSpe2.addEventListener("click",this);
      this._ctrSpe3.addEventListener("click",this);
      this._ctrSpe4.addEventListener("click",this);
      this._ctrSpe5.addEventListener("click",this);
      this._ctrSpe6.addEventListener("click",this);
      this._ctrSpe7.addEventListener("click",this);
      this._ctrSpe8.addEventListener("click",this);
      this._ctrSpe9.addEventListener("click",this);
      this._ctrSpe10.addEventListener("click",this);
      this._ctrSpe11.addEventListener("click",this);
      this._ctrSpe12.addEventListener("click",this);
      this._ctrAlignment.addEventListener("over",this);
      this._ctrJob0.addEventListener("over",this);
      this._ctrJob1.addEventListener("over",this);
      this._ctrJob2.addEventListener("over",this);
      this._ctrJob3.addEventListener("over",this);
      this._ctrJob4.addEventListener("over",this);
      this._ctrJob5.addEventListener("over",this);
      this._ctrJob6.addEventListener("over",this);
      this._ctrJob7.addEventListener("over",this);
      this._ctrJob8.addEventListener("over",this);
      this._ctrJob9.addEventListener("over",this);
      this._ctrJob10.addEventListener("over",this);
      this._ctrJob11.addEventListener("over",this);
      this._ctrJob12.addEventListener("over",this);
      this._ctrJob13.addEventListener("over",this);
      this._ctrJob14.addEventListener("over",this);
      this._ctrJob15.addEventListener("over",this);
      this._ctrJob16.addEventListener("over",this);
      this._ctrJob17.addEventListener("over",this);
      this._ctrJob18.addEventListener("over",this);
      this._ctrJob19.addEventListener("over",this);
      this._ctrJob20.addEventListener("over",this);
      this._ctrJob21.addEventListener("over",this);
      this._ctrJob22.addEventListener("over",this);
      this._ctrJob23.addEventListener("over",this);
      this._ctrSpe0.addEventListener("over",this);
      this._ctrSpe1.addEventListener("over",this);
      this._ctrSpe2.addEventListener("over",this);
      this._ctrSpe3.addEventListener("over",this);
      this._ctrSpe4.addEventListener("over",this);
      this._ctrSpe5.addEventListener("over",this);
      this._ctrSpe6.addEventListener("over",this);
      this._ctrSpe7.addEventListener("over",this);
      this._ctrSpe8.addEventListener("over",this);
      this._ctrSpe9.addEventListener("over",this);
      this._ctrSpe10.addEventListener("over",this);
      this._ctrSpe11.addEventListener("over",this);
      this._ctrSpe12.addEventListener("over",this);
      this._ctrAlignment.addEventListener("out",this);
      this._ctrJob0.addEventListener("out",this);
      this._ctrJob1.addEventListener("out",this);
      this._ctrJob2.addEventListener("out",this);
      this._ctrJob3.addEventListener("out",this);
      this._ctrJob4.addEventListener("out",this);
      this._ctrJob5.addEventListener("out",this);
      this._ctrJob6.addEventListener("out",this);
      this._ctrJob7.addEventListener("out",this);
      this._ctrJob8.addEventListener("out",this);
      this._ctrJob9.addEventListener("out",this);
      this._ctrJob10.addEventListener("out",this);
      this._ctrJob11.addEventListener("out",this);
      this._ctrJob12.addEventListener("out",this);
      this._ctrJob13.addEventListener("out",this);
      this._ctrJob14.addEventListener("out",this);
      this._ctrJob15.addEventListener("out",this);
      this._ctrJob16.addEventListener("out",this);
      this._ctrJob17.addEventListener("out",this);
      this._ctrJob18.addEventListener("out",this);
      this._ctrJob19.addEventListener("out",this);
      this._ctrJob20.addEventListener("out",this);
      this._ctrJob21.addEventListener("out",this);
      this._ctrJob22.addEventListener("out",this);
      this._ctrJob23.addEventListener("out",this);
      this._ctrSpe0.addEventListener("out",this);
      this._ctrSpe1.addEventListener("out",this);
      this._ctrSpe2.addEventListener("out",this);
      this._ctrSpe3.addEventListener("out",this);
      this._ctrSpe4.addEventListener("out",this);
      this._ctrSpe5.addEventListener("out",this);
      this._ctrSpe6.addEventListener("out",this);
      this._ctrSpe7.addEventListener("out",this);
      this._ctrSpe8.addEventListener("out",this);
      this._ctrSpe9.addEventListener("out",this);
      this._ctrSpe10.addEventListener("out",this);
      this._ctrSpe11.addEventListener("out",this);
      this._ctrSpe12.addEventListener("out",this);
      this._btn10.addEventListener("click",this);
      this._btn10.addEventListener("over",this);
      this._btn10.addEventListener("out",this);
      this._btn11.addEventListener("click",this);
      this._btn11.addEventListener("over",this);
      this._btn11.addEventListener("out",this);
      this._btn13.addEventListener("click",this);
      this._btn13.addEventListener("over",this);
      this._btn13.addEventListener("out",this);
      this._btn14.addEventListener("click",this);
      this._btn14.addEventListener("over",this);
      this._btn14.addEventListener("out",this);
      this._btn15.addEventListener("click",this);
      this._btn15.addEventListener("over",this);
      this._btn15.addEventListener("out",this);
      this.api.datacenter.Game.addEventListener("stateChanged",this);
      this._btnClose.addEventListener("click",this);
      this._btnClosePanel.addEventListener("click",this);
      this._btnCloseJobs.addEventListener("click",this);
      this._mcMoreStats.onRelease = function()
      {
         this._parent.click({target:this});
      };
      this._btnResetStats.addEventListener("click",this);
      this._btnResetStats.addEventListener("over",this);
      this._btnResetStats.addEventListener("out",this);
      this._cbModulatedLevel.addEventListener("itemSelected",this);
      this._mcMoreStats.onRollOver = function()
      {
         this._parent.gapi.showTooltip(this._parent.api.lang.getText("MORE_STATS"),this,-20);
      };
      this._mcMoreStats.onRollOut = function()
      {
         this._parent.gapi.hideTooltip();
      };
      this._mcAboutModulatedLevel.onRollOver = function()
      {
         this._parent.gapi.showTooltip(this._parent.api.lang.getText("ABOUT_MODULATED_LEVEL"),this,-20);
      };
      this._mcAboutModulatedLevel.onRollOut = function()
      {
         this._parent.gapi.hideTooltip();
      };
      this._mcAboutRestats.onRollOver = function()
      {
         this._parent.gapi.showTooltip(this._parent.api.lang.getText("ABOUT_RESET_STATS"),this,-20);
      };
      this._mcAboutRestats.onRollOut = function()
      {
         this._parent.gapi.hideTooltip();
      };
      this._mcAboutWisdom.onRollOver = function()
      {
         this._parent.gapi.showTooltip(this._parent.api.lang.getText("ABOUT_WISDOM"),this,-20);
      };
      this._mcAboutWisdom.onRollOut = function()
      {
         this._parent.gapi.hideTooltip();
      };
      var ref = this;
      this._lblVitality.onRollOver = function()
      {
         ref.over({target:this});
      };
      this._lblVitality.onRollOut = function()
      {
         ref.out({target:this});
      };
      this._lblVitalityValue.onRollOver = function()
      {
         ref.over({target:this});
      };
      this._lblVitalityValue.onRollOut = function()
      {
         ref.out({target:this});
      };
      this._lblWisdom.onRollOver = function()
      {
         ref.over({target:this});
      };
      this._lblWisdom.onRollOut = function()
      {
         ref.out({target:this});
      };
      this._lblWisdomValue.onRollOver = function()
      {
         ref.over({target:this});
      };
      this._lblWisdomValue.onRollOut = function()
      {
         ref.out({target:this});
      };
      this._lblForce.onRollOver = function()
      {
         ref.over({target:this});
      };
      this._lblForce.onRollOut = function()
      {
         ref.out({target:this});
      };
      this._lblForceValue.onRollOver = function()
      {
         ref.over({target:this});
      };
      this._lblForceValue.onRollOut = function()
      {
         ref.out({target:this});
      };
      this._lblIntelligence.onRollOver = function()
      {
         ref.over({target:this});
      };
      this._lblIntelligence.onRollOut = function()
      {
         ref.out({target:this});
      };
      this._lblIntelligenceValue.onRollOver = function()
      {
         ref.over({target:this});
      };
      this._lblIntelligenceValue.onRollOut = function()
      {
         ref.out({target:this});
      };
      this._lblChance.onRollOver = function()
      {
         ref.over({target:this});
      };
      this._lblChance.onRollOut = function()
      {
         ref.out({target:this});
      };
      this._lblChanceValue.onRollOver = function()
      {
         ref.over({target:this});
      };
      this._lblChanceValue.onRollOut = function()
      {
         ref.out({target:this});
      };
      this._lblAgility.onRollOver = function()
      {
         ref.over({target:this});
      };
      this._lblAgility.onRollOut = function()
      {
         ref.out({target:this});
      };
      this._lblAgilityValue.onRollOver = function()
      {
         ref.over({target:this});
      };
      this._lblAgilityValue.onRollOut = function()
      {
         ref.out({target:this});
      };
      this._btnJobOpen.addEventListener("click",this);
   }
   function initData()
   {
      var _loc2_ = this.api.datacenter.Player;
      this._ctrJob0._visible = false;
      this._ctrJob1._visible = false;
      this._ctrJob2._visible = false;
      this._ctrJob3._visible = false;
      this._ctrJob4._visible = false;
      this._ctrJob5._visible = false;
      this._ctrJob6._visible = false;
      this._ctrJob7._visible = false;
      this._ctrJob8._visible = false;
      this._ctrJob9._visible = false;
      this._ctrJob10._visible = false;
      this._ctrJob11._visible = false;
      this._ctrJob12._visible = false;
      this._ctrJob13._visible = false;
      this._ctrJob14._visible = false;
      this._ctrJob15._visible = false;
      this._ctrJob16._visible = false;
      this._ctrJob17._visible = false;
      this._ctrJob18._visible = false;
      this._ctrJob19._visible = false;
      this._ctrJob20._visible = false;
      this._ctrJob21._visible = false;
      this._ctrJob22._visible = false;
      this._ctrJob23._visible = false;
      this._ctrSpe0._visible = false;
      this._ctrSpe1._visible = false;
      this._ctrSpe2._visible = false;
      this._ctrSpe3._visible = false;
      this._ctrSpe4._visible = false;
      this._ctrSpe5._visible = false;
      this._ctrSpe6._visible = false;
      this._ctrSpe7._visible = false;
      this._ctrSpe8._visible = false;
      this._ctrSpe9._visible = false;
      this._ctrSpe10._visible = false;
      this._ctrSpe11._visible = false;
      this._ctrSpe12._visible = false;
      this._btnCloseJobs._visible = false;
      this._mcViewersPlacer2._visible = false;
      this.levelChanged({value:_loc2_.Level});
      this.xpChanged({value:_loc2_.XP});
      this.lpChanged({value:_loc2_.LP});
      this.lpMaxChanged({value:_loc2_.LPmax});
      this.apChanged({value:_loc2_.AP});
      this.mpChanged({value:_loc2_.MP});
      this.initiativeChanged({value:_loc2_.Initiative});
      this.discernmentChanged({value:_loc2_.Discernment});
      this.forceXtraChanged({value:_loc2_.ForceXtra});
      this.vitalityXtraChanged({value:_loc2_.VitalityXtra});
      this.wisdomXtraChanged({value:_loc2_.WisdomXtra});
      this.chanceXtraChanged({value:_loc2_.ChanceXtra});
      this.agilityXtraChanged({value:_loc2_.AgilityXtra});
      this.intelligenceXtraChanged({value:_loc2_.IntelligenceXtra});
      this.bonusPointsChanged({value:_loc2_.BonusPoints});
      this.energyChanged({value:_loc2_.Energy});
      this.alignmentChanged({alignment:_loc2_.alignment});
      var _loc3_ = this.api.datacenter.Player.Jobs;
      var _loc4_ = 0;
      var _loc5_;
      var _loc6_;
      var _loc7_;
      var _loc8_;
      var _loc9_;
      var _loc10_;
      while(_loc4_ < _loc3_.length)
      {
         _loc5_ = _loc3_[_loc4_];
         _loc6_ = _loc5_.specializationOf;
         if(_loc6_ != 0)
         {
            _loc7_ = 0;
            while(_loc7_ < 13)
            {
               _loc8_ = this["_ctrSpe" + _loc7_];
               if(_loc8_.contentData == undefined)
               {
                  _loc8_.contentData = _loc5_;
                  break;
               }
               _loc7_ += 1;
            }
         }
         else
         {
            _loc9_ = 0;
            while(_loc9_ < 24)
            {
               _loc10_ = this["_ctrJob" + _loc9_];
               if(_loc10_.contentData == undefined)
               {
                  _loc10_.contentData = _loc5_;
                  break;
               }
               _loc9_ += 1;
            }
         }
         _loc4_ += 1;
      }
      this._lblName.text = this.api.datacenter.Player.Name;
      this.activateBoostButtons(!this.api.datacenter.Game.isFight && this.api.datacenter.Map.bCanBoostStats);
      this._btnResetStats._visible = true;
      this._btnResetStats.enabled = !this.api.datacenter.Game.isFight;
      this._cbModulatedLevel.enabled = !this.api.datacenter.Game.isFight && !this.api.datacenter.Temporis.isCursedDungeonMap(this.api.datacenter.Map.id);
   }
   function initTexts()
   {
      this._lblEnergy.text = this.api.lang.getText("ENERGY");
      if(this.api.datacenter.Basics.aks_current_server.typeNum == dofus.datacenter.Server.SERVER_HARDCORE)
      {
         this._lblEnergy._alpha = 50;
         this._mcOverEnergy._visible = false;
      }
      this._lblCharacteristics.text = this.api.lang.getText("CHARACTERISTICS");
      var _loc2_ = this.api.lang.getText("MY_JOBS");
      this._lblMyJobs.text = _loc2_;
      this._btnJobOpen.label = _loc2_;
      this._mcViewersPlacer2.title = _loc2_;
      this._lblXP.text = this.api.lang.getText("EXPERIMENT");
      this._lblLP.text = this.api.lang.getText("LIFEPOINTS");
      this._lblAP.text = this.api.lang.getText("ACTIONPOINTS");
      this._lblMP.text = this.api.lang.getText("MOVEPOINTS");
      this._lblInitiative.text = this.api.lang.getText("INITIATIVE");
      this._lblDiscernment.text = this.api.lang.getText("DISCERNMENT");
      this._lblForce.text = this.api.lang.getText("FORCE");
      this._lblVitality.text = this.api.lang.getText("VITALITY");
      this._lblWisdom.text = this.api.lang.getText("WISDOM");
      this._lblChance.text = this.api.lang.getText("CHANCE");
      this._lblAgility.text = this.api.lang.getText("AGILITY");
      this._lblIntelligence.text = this.api.lang.getText("INTELLIGENCE");
      this._lblCapital.text = this.api.lang.getText("CHARACTERISTICS_POINTS");
      this._lblSpecialization.text = this.api.lang.getText("SPECIALIZATIONS");
   }
   function initTemporis()
   {
      var _loc2_ = this.api.datacenter.Basics.aks_current_server.isTemporis();
      this._mcAboutWisdom._visible = _loc2_;
      this._mcAboutRestats._visible = true;
      this._btnResetStats.enabled = !this.api.datacenter.Game.isFight;
      this._cbModulatedLevel._visible = _loc2_;
      this._mcAboutModulatedLevel._visible = _loc2_;
      this._cbModulatedLevel.enabled = !this.api.datacenter.Game.isFight && !this.api.datacenter.Temporis.isCursedDungeonMap(this.api.datacenter.Map.id);
      this.showJobsPanel(false);
   }
   function getStatsCostString(oBoost)
   {
      return "<b><u>" + this.api.lang.getText("COST") + " :" + "</u> " + oBoost.cost + "</b> " + this.api.lang.getText("POUR") + " <b>" + oBoost.count + "</b>";
   }
   function showStats()
   {
      this.hideAlignment();
      this.hideJob();
      if(this._svStats == undefined)
      {
         this.attachMovie("StatsViewer","_svStats",this.getNextHighestDepth(),{_x:this._mcViewersPlacer._x,_y:this._mcViewersPlacer._y});
         this._btnClosePanel._visible = true;
         this._btnClosePanel.swapDepths(this.getNextHighestDepth());
         this._btnClosePanel._x += 35;
      }
      else
      {
         this.hideStats();
      }
   }
   function hideStats()
   {
      if(this._svStats != undefined)
      {
         this._btnClosePanel._x -= 35;
         this._svStats.removeMovieClip();
      }
      this._btnClosePanel._visible = false;
   }
   function showJob(oJob, oContainer)
   {
      this.hideAlignment();
      this.hideStats();
      if(oJob == undefined)
      {
         this.hideJob();
         return undefined;
      }
      if(this._oCurrentJob == oJob)
      {
         this.hideJob();
         return undefined;
      }
      if(oContainer != undefined)
      {
         oJob._container = oContainer;
      }
      if(this._jvJob == undefined)
      {
         this.attachMovie("JobViewer","_jvJob",this.getNextHighestDepth(),{_x:this._mcViewersPlacer._x,_y:this._mcViewersPlacer._y});
      }
      this._btnClosePanel._visible = true;
      this._btnClosePanel.swapDepths(this.getNextHighestDepth());
      this._jvJob.job = oJob;
      if(this._oCurrentJob != undefined && this._oCurrentJob._container != undefined)
      {
         this._oCurrentJob._container.selected = false;
      }
      if(oJob._container != undefined)
      {
         oJob._container.selected = true;
      }
      this._oCurrentJob = oJob;
   }
   function hideJob()
   {
      if(this._oCurrentJob != undefined && this._oCurrentJob._container != undefined)
      {
         this._oCurrentJob._container.selected = false;
      }
      if(this._jvJob != undefined)
      {
         this._jvJob.removeMovieClip();
      }
      delete this._oCurrentJob;
      this._btnClosePanel._visible = false;
   }
   function showAlignment()
   {
      this.hideJob();
      this.hideStats();
      if(this._avAlignment == undefined)
      {
         this.attachMovie("AlignmentViewer","_avAlignment",this.getNextHighestDepth(),{_x:this._mcViewersPlacer._x,_y:this._mcViewersPlacer._y});
         this._btnClosePanel._visible = true;
         this._btnClosePanel.swapDepths(this.getNextHighestDepth());
      }
      else
      {
         this.hideAlignment();
      }
   }
   function hideAlignment()
   {
      if(this._avAlignment != undefined)
      {
         this._avAlignment.removeMovieClip();
      }
      this._btnClosePanel._visible = false;
   }
   function showJobsPanel(booljob)
   {
      this._ctrJob0._visible = booljob;
      this._ctrJob1._visible = booljob;
      this._ctrJob2._visible = booljob;
      this._ctrJob3._visible = booljob;
      this._ctrJob4._visible = booljob;
      this._ctrJob5._visible = booljob;
      this._ctrJob6._visible = booljob;
      this._ctrJob7._visible = booljob;
      this._ctrJob8._visible = booljob;
      this._ctrJob9._visible = booljob;
      this._ctrJob10._visible = booljob;
      this._ctrJob11._visible = booljob;
      this._ctrJob12._visible = booljob;
      this._ctrJob13._visible = booljob;
      this._ctrJob14._visible = booljob;
      this._ctrJob15._visible = booljob;
      this._ctrJob16._visible = booljob;
      this._ctrJob17._visible = booljob;
      this._ctrJob18._visible = booljob;
      this._ctrJob19._visible = booljob;
      this._ctrJob20._visible = booljob;
      this._ctrJob21._visible = booljob;
      this._ctrJob22._visible = booljob;
      this._ctrJob23._visible = booljob;
      this._ctrSpe0._visible = booljob;
      this._ctrSpe1._visible = booljob;
      this._ctrSpe2._visible = booljob;
      this._ctrSpe3._visible = booljob;
      this._ctrSpe4._visible = booljob;
      this._ctrSpe5._visible = booljob;
      this._ctrSpe6._visible = booljob;
      this._ctrSpe7._visible = booljob;
      this._ctrSpe8._visible = booljob;
      this._ctrSpe9._visible = booljob;
      this._ctrSpe10._visible = booljob;
      this._ctrSpe11._visible = booljob;
      this._ctrSpe12._visible = booljob;
      this._btnCloseJobs._visible = booljob;
      this._mcViewersPlacer2._visible = booljob;
   }
   function activateBoostButtons(bActivated)
   {
      var _loc3_ = 10;
      while(_loc3_ < 16)
      {
         if(_loc3_ != 12)
         {
            this["_btn" + _loc3_].enabled = bActivated;
         }
         _loc3_ += 1;
      }
      if(this._btn12 != undefined)
      {
         this._btn12.enabled = false;
         this._btn12._visible = false;
      }
   }
   function updateCharacteristicButton(nCharacteristicID)
   {
      if(nCharacteristicID == 12)
      {
         if(this._btn12 != undefined)
         {
            this._btn12.enabled = false;
            this._btn12._visible = false;
         }
         return undefined;
      }
      var _loc3_ = this.api.datacenter.Player.getBoostCostAndCountForCharacteristic(nCharacteristicID).cost;
      var _loc4_ = this["_btn" + nCharacteristicID];
      if(_loc3_ <= this.api.datacenter.Player.BonusPoints)
      {
         _loc4_._visible = true;
      }
      else
      {
         _loc4_._visible = false;
      }
   }
   function click(_loc2_)
   {
      var _loc3_;
      var _loc4_;
      var _loc5_;
      var _loc6_;
      var _loc7_;
      var _loc8_;
      switch(_loc2_.target._name)
      {
         case "_btnClosePanel":
            this.hideJob();
            this.hideAlignment();
            this.hideStats();
            return undefined;
         case "_ctrAlignment":
            if(this.api.datacenter.Player.data.alignment.index == 0)
            {
               this.api.kernel.showMessage(undefined,this.api.lang.getText("NEED_ALIGNMENT"),"ERROR_BOX");
               return undefined;
            }
            this.showAlignment();
            return undefined;
            break;
         case "_btnClose":
            this.callClose();
            return undefined;
         case "_mcMoreStats":
            this.showStats();
            return undefined;
         case "_btn10":
         case "_btn11":
         case "_btn13":
         case "_btn14":
         case "_btn15":
            this.api.sounds.events.onStatsJobBoostButtonClick();
            _loc3_ = Number(_loc2_.target._name.substr(4));
            if(this.api.datacenter.Player.canBoost(_loc3_))
            {
               var oBoost = this.api.datacenter.Player.getBoostCostAndCountForCharacteristic(_loc3_);
               var nCost = oBoost.cost;
               _loc4_ = oBoost.possibleCount;
               var nCapital = this.api.datacenter.Player.BonusPoints;
               if(Key.isDown(Key.CONTROL) || Key.isDown(Key.SHIFT))
               {
                  _loc5_ = "POPUP_QUANTITY_STATS_BOOST_DESCRIPTION";
                  _loc6_ = [this.getStatsCostString(oBoost),function(nMin, nMax, nValue)
                  {
                     return String(nValue * nCost);
                  },function(nMin, nMax, nValue)
                  {
                     return String(nCapital - nValue * nCost);
                  },function(nMin, nMax, nValue)
                  {
                     return String(nValue * oBoost.count);
                  }];
                  _loc7_ = this.gapi.loadUIComponent("PopupQuantityWithDescription","PopupQuantity",{descriptionLangKey:_loc5_,descriptionLangKeyParams:_loc6_,value:1,max:_loc4_,isMaxButtonValidationEnabled:false,params:{targetType:"charac",characteristicID:_loc3_}});
                  _loc7_.addEventListener("validate",this);
                  this._popupQuantity = _loc7_;
                  return undefined;
               }
               this.api.network.Account.boost(_loc3_,1);
               return undefined;
            }
            return undefined;
            break;
         case "_btnResetStats":
            _loc8_ = this.gapi.loadUIComponent("AskYesNo","AskYesNoRestat",{title:this.api.lang.getText("RESET_STATS"),text:this.api.lang.getText("CONFIRM_RESET_STATS")});
            _loc8_.addEventListener("yes",this);
            return undefined;
         case "_btnJobOpen":
            if(this._mcViewersPlacer2._visible)
            {
               this.showJobsPanel(false);
               return undefined;
            }
            this.showJobsPanel(true);
            return undefined;
            break;
         case "_btnCloseJobs":
            this.showJobsPanel(false);
            return undefined;
         default:
            if(this._mcViewersPlacer2._visible && _loc2_.target.contentData != undefined)
            {
               this.showJob(_loc2_.target.contentData,_loc2_.target);
               return undefined;
            }
            return undefined;
      }
   }
   function validate(oEvent)
   {
      var _loc3_ = oEvent.value;
      var _loc4_ = null;
      var _loc5_;
      if((_loc4_ = oEvent.params.targetType) === "charac")
      {
         _loc5_ = oEvent.params.characteristicID;
         this.api.network.Account.boost(_loc5_,_loc3_);
      }
   }
   function over(oEvent)
   {
      var _loc3_;
      var _loc4_;
      var _loc5_;
      var _loc6_;
      var _loc7_;
      var _loc8_;
      var _loc9_;
      var _loc10_;
      var _loc11_;
      var _loc12_;
      var _loc13_;
      var _loc14_;
      var _loc15_;
      var _loc16_;
      var _loc17_;
      var _loc18_;
      var _loc19_;
      var _loc20_;
      var _loc21_;
      var _loc22_;
      switch(oEvent.target._name)
      {
         case "_btn10":
         case "_btn11":
         case "_btn13":
         case "_btn14":
         case "_btn15":
            _loc3_ = Number(oEvent.target._name.substr(4));
            _loc4_ = this.api.datacenter.Player.getBoostCostAndCountForCharacteristic(_loc3_);
            this.gapi.showTooltip(this.getStatsCostString(_loc4_),oEvent.target,-20);
            return undefined;
         case "_ctrAlignment":
            this.gapi.showTooltip(this.api.datacenter.Player.alignment.name,oEvent.target,oEvent.target.height + 5);
            return undefined;
         case "_btnResetStats":
            this.gapi.showTooltip(this.api.lang.getText("RESET_STATS"),oEvent.target,-20);
            return undefined;
         case "_lblVitality":
            this.gapi.showTooltip(this.api.lang.getText("HELP_VITALITY"),oEvent.target,20);
            return undefined;
         case "_lblWisdom":
            this.gapi.showTooltip(this.api.lang.getText("HELP_WISDOM"),oEvent.target,20);
            return undefined;
         case "_lblIntelligence":
            this.gapi.showTooltip(this.api.lang.getText("HELP_INTELLIGENCE"),oEvent.target,20);
            return undefined;
         case "_lblForce":
            this.gapi.showTooltip(this.api.lang.getText("HELP_FORCE"),oEvent.target,20);
            return undefined;
         case "_lblChance":
            this.gapi.showTooltip(this.api.lang.getText("HELP_CHANCE"),oEvent.target,20);
            return undefined;
         case "_lblAgility":
            this.gapi.showTooltip(this.api.lang.getText("HELP_AGILITY"),oEvent.target,20);
            return undefined;
         case "_lblVitalityValue":
            _loc5_ = 11;
            _loc6_ = this.api.datacenter.Player.getStatDetail(_loc5_);
            _loc7_ = "Base : " + (_loc6_.s - _loc6_.a) + "\nObjets : " + _loc6_.i + "\nBonus : " + (_loc6_.d + _loc6_.b);
            this.gapi.showTooltip(_loc7_,oEvent.target,20);
            return undefined;
         case "_lblWisdomValue":
            _loc8_ = 12;
            _loc9_ = this.api.datacenter.Player.getStatDetail(_loc8_);
            _loc10_ = "Base : " + (_loc9_.s - _loc9_.a) + "\nObjets : " + _loc9_.i + "\nBonus : " + (_loc9_.d + _loc9_.b);
            this.gapi.showTooltip(_loc10_,oEvent.target,20);
            return undefined;
         case "_lblIntelligenceValue":
            _loc11_ = 15;
            _loc12_ = this.api.datacenter.Player.getStatDetail(_loc11_);
            _loc13_ = "Base : " + (_loc12_.s - _loc12_.a) + "\nObjets : " + _loc12_.i + "\nBonus : " + (_loc12_.d + _loc12_.b);
            this.gapi.showTooltip(_loc13_,oEvent.target,20);
            return undefined;
         case "_lblForceValue":
            _loc14_ = 10;
            _loc15_ = this.api.datacenter.Player.getStatDetail(_loc14_);
            _loc16_ = "Base : " + (_loc15_.s - _loc15_.a) + "\nObjets : " + _loc15_.i + "\nBonus : " + (_loc15_.d + _loc15_.b);
            this.gapi.showTooltip(_loc16_,oEvent.target,20);
            return undefined;
         case "_lblChanceValue":
            _loc17_ = 13;
            _loc18_ = this.api.datacenter.Player.getStatDetail(_loc17_);
            _loc19_ = "Base : " + (_loc18_.s - _loc18_.a) + "\nObjets : " + _loc18_.i + "\nBonus : " + (_loc18_.d + _loc18_.b);
            this.gapi.showTooltip(_loc19_,oEvent.target,20);
            return undefined;
         case "_lblAgilityValue":
            _loc20_ = 14;
            _loc21_ = this.api.datacenter.Player.getStatDetail(_loc20_);
            _loc22_ = "Base : " + (_loc21_.s - _loc21_.a) + "\nObjets : " + _loc21_.i + "\nBonus : " + (_loc21_.d + _loc21_.b);
            this.gapi.showTooltip(_loc22_,oEvent.target,20);
            return undefined;
         default:
            if(oEvent.target.contentData != undefined)
            {
               this.gapi.showTooltip(oEvent.target.contentData.name,oEvent.target,-20);
               return undefined;
            }
            return undefined;
      }
   }
   function out(oEvent)
   {
      this.gapi.hideTooltip();
   }
   function itemSelected(oEvent)
   {
      var _loc3_ = null;
      var _loc4_;
      if((_loc3_ = oEvent.target) === this._cbModulatedLevel)
      {
         _loc4_ = this._cbModulatedLevel.selectedItem;
         if(_loc4_.id == 0)
         {
            this.api.network.Temporis.episodeThree.askChangeLevel("real");
         }
         else
         {
            this.api.network.Temporis.episodeThree.askChangeLevel("" + _loc4_.id * 30);
         }
      }
   }
   function yes(oEvent)
   {
      this.api.network.send("Apc");
   }
   function nameChanged(oEvent)
   {
      this._lblName.text = oEvent.value;
   }
   function levelChanged(oEvent)
   {
      this._lblLevel.text = this.api.lang.getText("LEVEL") + " " + this.api.datacenter.Player.ShowedLevel;
      var _loc3_ = new ank.utils.ExtendedArray();
      _loc3_.push({label:this.api.lang.getText("ACTUAL_LEVEL"),id:0});
      var _loc4_ = 1;
      var _loc5_;
      while(_loc4_ < 7)
      {
         _loc5_ = _loc4_ * 30;
         if(_loc5_ >= this.api.datacenter.Player.Level)
         {
            break;
         }
         _loc3_.push({label:"" + _loc5_,id:_loc4_});
         _loc4_ += 1;
      }
      this._cbModulatedLevel.dataProvider = _loc3_;
      var _loc6_;
      var _loc7_;
      if(this.api.datacenter.Player.Level == this.api.datacenter.Player.ShowedLevel)
      {
         this._cbModulatedLevel.selectedIndex = 0;
      }
      else
      {
         _loc6_ = 1;
         while(_loc6_ < 7)
         {
            _loc7_ = _loc6_ * 30;
            if(_loc7_ == this.api.datacenter.Player.ShowedLevel)
            {
               this._cbModulatedLevel.selectedIndex = _loc6_;
            }
            _loc6_ += 1;
         }
      }
   }
   function xpChanged(oEvent)
   {
      var _loc2_;
      if(this.api.datacenter.Player.XPhigh <= this.api.datacenter.Player.XPlow)
      {
         this._pbXP.minimum = 0;
         this._pbXP.maximum = 100;
         _loc2_ = 100;
      }
      else
      {
         this._pbXP.minimum = this.api.datacenter.Player.XPlow;
         _loc2_ = this.api.datacenter.Player.XPhigh;
         this._pbXP.maximum = _loc2_;
      }
      var _loc3_ = this;
      this._mcXP.onRollOver = function()
      {
         if(_loc3_.api.datacenter.Player.XPhigh <= _loc3_.api.datacenter.Player.XPlow)
         {
            _loc3_.gapi.showTooltip(_loc3_.api.lang.getText("LEVEL") + " max",this,-10);
         }
         else
         {
            this._parent.gapi.showTooltip(new ank.utils.ExtendedString(oEvent.value).addMiddleChar(this._parent.api.lang.getConfigText("THOUSAND_SEPARATOR"),3) + " / " + new ank.utils.ExtendedString(this._parent._pbXP.maximum).addMiddleChar(this._parent.api.lang.getConfigText("THOUSAND_SEPARATOR"),3),this,-10);
         }
      };
      this._mcXP.onRollOut = function()
      {
         this._parent.gapi.hideTooltip();
      };
   }
   function lpChanged(oEvent)
   {
      this._lblLPValue.text = String(oEvent.value);
   }
   function lpMaxChanged(oEvent)
   {
      this._lblLPmaxValue.text = String(oEvent.value);
   }
   function apChanged(oEvent)
   {
      this._lblAPValue.text = String(Math.max(0,oEvent.value));
   }
   function mpChanged(oEvent)
   {
      this._lblMPValue.text = String(Math.max(0,oEvent.value));
   }
   function forceXtraChanged(oEvent)
   {
      this._lblForceValue.text = this.api.datacenter.Player.Force + (oEvent.value == 0 ? "" : (oEvent.value <= 0 ? " (" : " (+") + String(oEvent.value) + ")");
      this.updateCharacteristicButton(10);
   }
   function vitalityXtraChanged(oEvent)
   {
      this._lblVitalityValue.text = this.api.datacenter.Player.Vitality + (oEvent.value == 0 ? "" : (oEvent.value <= 0 ? " (" : " (+") + String(oEvent.value) + ")");
      this.updateCharacteristicButton(11);
   }
   function wisdomXtraChanged(oEvent)
   {
      this._lblWisdomValue.text = this.api.datacenter.Player.Wisdom + (oEvent.value == 0 ? "" : (oEvent.value <= 0 ? " (" : " (+") + String(oEvent.value) + ")");
   }
   function chanceXtraChanged(oEvent)
   {
      this._lblChanceValue.text = this.api.datacenter.Player.Chance + (oEvent.value == 0 ? "" : (oEvent.value <= 0 ? " (" : " (+") + String(oEvent.value) + ")");
      this.updateCharacteristicButton(13);
   }
   function agilityXtraChanged(oEvent)
   {
      this._lblAgilityValue.text = this.api.datacenter.Player.Agility + (oEvent.value == 0 ? "" : (oEvent.value <= 0 ? " (" : " (+") + String(oEvent.value) + ")");
      this.updateCharacteristicButton(14);
   }
   function intelligenceXtraChanged(oEvent)
   {
      this._lblIntelligenceValue.text = this.api.datacenter.Player.Intelligence + (oEvent.value == 0 ? "" : (oEvent.value <= 0 ? " (" : " (+") + String(oEvent.value) + ")");
      this.updateCharacteristicButton(15);
   }
   function bonusPointsChanged(oEvent)
   {
      this._lblCapitalValue.text = String(oEvent.value);
   }
   function energyChanged(oEvent)
   {
      if(this.api.datacenter.Basics.aks_current_server.typeNum != dofus.datacenter.Server.SERVER_HARDCORE)
      {
         this._mcEnergy.onRollOver = function()
         {
            this._parent.gapi.showTooltip(new ank.utils.ExtendedString(oEvent.value).addMiddleChar(this._parent.api.lang.getConfigText("THOUSAND_SEPARATOR"),3) + " / " + new ank.utils.ExtendedString(Math.max(10000,this._parent._pbEnergy.maximum)).addMiddleChar(this._parent.api.lang.getConfigText("THOUSAND_SEPARATOR"),3),this,-10);
         };
         this._mcEnergy.onRollOut = function()
         {
            this._parent.gapi.hideTooltip();
         };
         this._pbEnergy.maximum = this.api.datacenter.Player.EnergyMax;
         this._pbEnergy.value = oEvent.value;
      }
      else
      {
         this._pbEnergy._alpha = 50;
         this._pbEnergy.enabled = false;
      }
   }
   function energyMaxChanged(oEvent)
   {
      this._pbEnergy.maximum = oEvent.value;
   }
   function alignmentChanged(oEvent)
   {
      this._ctrAlignment.contentPath = oEvent.alignment.iconFile;
   }
   function initiativeChanged(oEvent)
   {
      this._lblInitiativeValue.text = String(oEvent.value);
   }
   function discernmentChanged(oEvent)
   {
      this._lblDiscernmentValue.text = String(oEvent.value);
   }
   function stateChanged(oEvent)
   {
      this.activateBoostButtons(!(oEvent.value > 1 && oEvent.value != undefined));
   }
}
