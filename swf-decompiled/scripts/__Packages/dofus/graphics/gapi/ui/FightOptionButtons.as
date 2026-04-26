class dofus.graphics.gapi.ui.FightOptionButtons extends dofus.graphics.gapi.core.DofusAdvancedComponent
{
   var _btnBlockJoiner;
   var _btnBlockJoinerExceptParty;
   var _btnBlockSpectators;
   var _btnCreatureMode;
   var _btnFlag;
   var _btnHelp;
   var _btnTactic;
   var _btnToggleSprites;
   var addToQueue;
   var gapi;
   static var CLASS_NAME = "FightOptionButtons";
   function FightOptionButtons()
   {
      super();
   }
   function init()
   {
      super.init(false,dofus.graphics.gapi.ui.FightOptionButtons.CLASS_NAME);
   }
   function createChildren()
   {
      this.addToQueue({object:this,method:this.addListeners});
      this.addToQueue({object:this,method:this.initOption});
      this.addToQueue({object:this,method:this.initData});
   }
   function addListeners()
   {
      this._btnTactic.addEventListener("click",this);
      this._btnTactic.addEventListener("over",this);
      this._btnTactic.addEventListener("out",this);
      this._btnFlag.addEventListener("click",this);
      this._btnFlag.addEventListener("over",this);
      this._btnFlag.addEventListener("out",this);
      this._btnBlockJoinerExceptParty.addEventListener("click",this);
      this._btnBlockJoinerExceptParty.addEventListener("over",this);
      this._btnBlockJoinerExceptParty.addEventListener("out",this);
      this._btnBlockJoiner.addEventListener("click",this);
      this._btnBlockJoiner.addEventListener("over",this);
      this._btnBlockJoiner.addEventListener("out",this);
      this._btnHelp.addEventListener("click",this);
      this._btnHelp.addEventListener("over",this);
      this._btnHelp.addEventListener("out",this);
      this._btnBlockSpectators.addEventListener("click",this);
      this._btnBlockSpectators.addEventListener("over",this);
      this._btnBlockSpectators.addEventListener("out",this);
      this._btnToggleSprites.addEventListener("click",this);
      this._btnToggleSprites.addEventListener("over",this);
      this._btnToggleSprites.addEventListener("out",this);
      this._btnCreatureMode.addEventListener("click",this);
      this._btnCreatureMode.addEventListener("over",this);
      this._btnCreatureMode.addEventListener("out",this);
   }
   function initData()
   {
      if(!this.api.datacenter.Game.isSpectator)
      {
         if(this.api.datacenter.Game.isRunning)
         {
            this.onGameRunning();
            return undefined;
         }
         if(!this.api.datacenter.Player.inParty)
         {
            this._btnBlockJoinerExceptParty._visible = false;
            this._btnTactic._x = 642;
            this._btnCreatureMode._x = 622;
         }
         else
         {
            if(this._btnBlockJoinerExceptParty.selected)
            {
               this.api.network.Fights.blockJoinerExceptParty();
            }
            this._btnTactic._x = 622;
         }
      }
      else
      {
         this._btnBlockJoinerExceptParty._visible = false;
         this._btnBlockJoiner._visible = false;
         this._btnHelp._visible = false;
         this._btnBlockSpectators._visible = false;
         this._btnFlag._visible = false;
         this._btnTactic._x = 722;
         this._btnCreatureMode._x = 702;
      }
      this._btnTactic.selected = this.api.datacenter.Game.isTacticMode;
      this._btnToggleSprites._visible = false;
      this._btnCreatureMode.selected = _global.CRIATURA;
      if(this._btnBlockSpectators.selected)
      {
         this.api.network.Fights.blockSpectators();
      }
      if(this._btnBlockJoiner.selected)
      {
         this.api.network.Fights.blockJoiner();
      }
   }
   function initOption()
   {
      this._btnBlockJoinerExceptParty.selected = this.api.kernel.OptionsManager.getOption("FightGroupAutoLock");
      this._btnBlockSpectators.selected = this.api.datacenter.Game.isSpectatorBlocked;
      this._btnBlockJoiner.selected = this.api.datacenter.Game.isFightBlocked;
   }
   function onGameRunning()
   {
      this._btnBlockJoinerExceptParty._visible = false;
      this._btnBlockJoiner._visible = false;
      this._btnHelp._visible = false;
      this._btnToggleSprites._visible = true;
      this._btnTactic._x = 662;
      this._btnCreatureMode._x = 642;
      this._btnCreatureMode._visible = true;
   }
   function click(oEvent)
   {
      var _loc4_;
      var _loc5_;
      var _loc6_;
      var _loc7_;
      var _loc8_;
      switch(oEvent.target)
      {
         case this._btnTactic:
            _loc4_ = !this.api.datacenter.Game.isTacticMode;
            this.api.datacenter.Game.isTacticMode = _loc4_;
            this.api.gfx.activateTacticMode(this.api,_loc4_);
            return undefined;
         case this._btnFlag:
            this.api.kernel.GameManager.switchToFlagSet();
            return undefined;
         case this._btnBlockJoinerExceptParty:
            this.api.network.Fights.blockJoinerExceptParty();
            return undefined;
         case this._btnCreatureMode:
            _global.CRIATURA = !_global.CRIATURA;
            this.api.network.GameActions.onModoCriatura();
            return undefined;
         case this._btnBlockJoiner:
            _loc5_ = !this.api.datacenter.Game.isFightBlocked;
            this.api.datacenter.Game.isFightBlocked = _loc5_;
            this.api.network.Fights.blockJoiner();
            return undefined;
         case this._btnHelp:
            _loc6_ = !this.api.datacenter.Game.isNeedingHelp;
            this.api.datacenter.Game.isNeedingHelp = _loc6_;
            this.api.network.Fights.needHelp();
            return undefined;
         case this._btnBlockSpectators:
            if(dofus.datacenter.DofusMap.isTournament(this.api.datacenter.Map.id))
            {
               _loc7_ = this.gapi.loadUIComponent("AskYesNo","AskYesNoDisableSpectator",{title:this.api.lang.getText("QUESTION"),text:this.api.lang.getText("FIGHT_OPTION_SPECTATOR") + " ?"});
               _loc7_.addEventListener("yes",this);
               _loc7_.addEventListener("no",this);
               return undefined;
            }
            _loc8_ = !this.api.datacenter.Game.isSpectatorBlocked;
            this.api.datacenter.Game.isSpectatorBlocked = _loc8_;
            this.api.network.Fights.blockSpectators();
            return undefined;
            break;
         case this._btnToggleSprites:
            this.api.datacenter.Basics.gfx_isSpritesHidden = !this.api.datacenter.Basics.gfx_isSpritesHidden;
            if(this.api.datacenter.Basics.gfx_isSpritesHidden)
            {
               this.api.gfx.spriteHandler.maskAllSprites();
               break;
            }
            this.api.gfx.spriteHandler.unmaskAllSprites();
      }
      return undefined;
   }
   function yes()
   {
      var _loc2_ = !this.api.datacenter.Game.isSpectatorBlocked;
      this.api.datacenter.Game.isSpectatorBlocked = _loc2_;
      this.api.network.Fights.blockSpectators();
   }
   function no()
   {
      this._btnBlockSpectators.selected = !this._btnBlockSpectators.selected;
   }
   function over(oEvent)
   {
      switch(oEvent.target)
      {
         case this._btnTactic:
            this.gapi.showTooltip(this.api.lang.getText("TACTIC_MODE"),this._btnFlag,-30);
            return undefined;
         case this._btnCreatureMode:
            this.gapi.showTooltip(this.api.lang.getText("CREATURA_MODE"),this._btnCreatureMode,-30);
            return undefined;
         case this._btnFlag:
            this.gapi.showTooltip(this.api.lang.getText("FLAG_INDICATOR_HELP"),this._btnFlag,-30);
            return undefined;
         case this._btnBlockJoinerExceptParty:
            this.gapi.showTooltip(this.api.lang.getText("FIGHT_OPTION_BLOCKJOINEREXCEPTPARTY"),this._btnFlag,-30);
            return undefined;
         case this._btnBlockJoiner:
            this.gapi.showTooltip(this.api.lang.getText("FIGHT_OPTION_BLOCKJOINER"),this._btnFlag,-30);
            return undefined;
         case this._btnHelp:
            this.gapi.showTooltip(this.api.lang.getText("FIGHT_OPTION_HELP"),this._btnFlag,-30);
            return undefined;
         case this._btnBlockSpectators:
            this.gapi.showTooltip(this.api.lang.getText("FIGHT_OPTION_SPECTATOR"),this._btnFlag,-30);
            return undefined;
         case this._btnToggleSprites:
            this.gapi.showTooltip(this.api.lang.getText("FIGHT_OPTION_SPRITES"),this._btnFlag,-30);
      }
      return undefined;
   }
   function out(oEvent)
   {
      this.gapi.hideTooltip();
   }
   function moveButtons(nDistance)
   {
      this._btnTactic._y += nDistance;
      this._btnFlag._y += nDistance;
      this._btnBlockJoinerExceptParty._y += nDistance;
      this._btnBlockJoiner._y += nDistance;
      this._btnHelp._y += nDistance;
      this._btnBlockSpectators._y += nDistance;
      this._btnToggleSprites._y += nDistance;
   }
}
