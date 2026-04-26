class dofus.datacenter.PlayableCharacter extends ank.battlefield.datacenter.Sprite
{
   var CharacteristicsManager;
   var EffectsManager;
   var GameActionsManager;
   var __proto__;
   var _aAccessories;
   var _ap;
   var _apinit;
   var _gfxID;
   var _kama;
   var _level;
   var _lp;
   var _lpmax;
   var _mp;
   var _mpinit;
   var _nPvpGain;
   var _name;
   var _team;
   var _xp;
   var api;
   var broadcastMessage;
   var dispatchEvent;
   var id;
   var mc;
   var _summoned = false;
   function PlayableCharacter(sID, clipClass, sGfxFile, cellNum, dir, gfxID)
   {
      super();
      if(this.__proto__ == dofus.datacenter.PlayableCharacter.prototype)
      {
         this.initialize(sID,clipClass,sGfxFile,cellNum,dir,gfxID);
      }
   }
   function initialize(sID, clipClass, sGfxFile, cellNum, dir, gfxID)
   {
      super.initialize(sID,clipClass,sGfxFile,cellNum,dir);
      this.api = _global.API;
      this._gfxID = gfxID;
      this.GameActionsManager = new dofus.managers.GameActionsManager(this,this.api);
      this.CharacteristicsManager = new dofus.managers.CharacteristicsManager(this,this.api);
      this.EffectsManager = new dofus.managers.EffectsManager(this,this.api);
      if(sID == this.api.datacenter.Player.ID)
      {
         this._ap = this.api.datacenter.Player.AP;
         this._mp = this.api.datacenter.Player.MP;
      }
      AsBroadcaster.initialize(this);
      mx.events.EventDispatcher.initialize(this);
   }
   function updateLP(dLP)
   {
      this.LP += Number(dLP);
      if(dLP < 0 && this.api.datacenter.Game.isFight)
      {
         this.LPmax -= Math.floor((- dLP) * this.api.lang.getConfigText("PERMANENT_DAMAGE"));
         if(this.api.datacenter.Player.ID == this.id)
         {
            this.api.datacenter.Player.LPmax = this.LPmax;
            this.api.ui.getUIComponent("Banner").lpMaxChanged({value:this.LPmax});
            this.api.ui.getUIComponent("StatJob").lpMaxChanged({value:this.LPmax});
         }
         this.api.ui.getUIComponent("Timeline").timelineControl.updateCharacters();
      }
      this.api.gfx.fightPointAnimManager.addLifePointAnim(this.id,dLP);
      if(dLP < 0 && (!this.api.datacenter.Player.isSkippingFightAnimations && this.api.electron.isWindowFocused))
      {
         this.mc.setAnim("Hit");
      }
   }
   function updateAP(dAP, bUsed)
   {
      if(bUsed == undefined)
      {
         bUsed = false;
      }
      if(this.api.datacenter.Game.currentPlayerID != this.id && bUsed)
      {
         return undefined;
      }
      this.AP += Number(dAP);
      this.AP = Math.max(0,this.AP);
      this.api.gfx.fightPointAnimManager.addActionPointAnim(this.id,dAP);
   }
   function initAP(bWithModerator)
   {
      if(bWithModerator == undefined)
      {
         bWithModerator = true;
      }
      var _loc3_;
      if(bWithModerator)
      {
         _loc3_ = this.CharacteristicsManager.getModeratorValue("1");
         this.AP = Number(this.APinit) + Number(_loc3_);
      }
      else
      {
         this.AP = Number(this.APinit);
      }
   }
   function updateMP(dMP, bUsed)
   {
      if(bUsed == undefined)
      {
         bUsed = false;
      }
      if(this.api.datacenter.Game.currentPlayerID != this.id && bUsed)
      {
         return undefined;
      }
      var _loc4_ = Number(dMP);
      this.MP += _loc4_;
      this.MP = Math.max(0,this.MP);
      this.api.gfx.fightPointAnimManager.addMovePointAnim(this.id,_loc4_);
   }
   function initMP(bWithModerator)
   {
      if(bWithModerator == undefined)
      {
         bWithModerator = true;
      }
      var _loc3_;
      if(bWithModerator)
      {
         _loc3_ = this.CharacteristicsManager.getModeratorValue("23");
         this.MP = Number(this.MPinit) + Number(_loc3_);
      }
      else
      {
         this.MP = Number(this.MPinit);
      }
   }
   function get gfxID()
   {
      return this._gfxID;
   }
   function set gfxID(value)
   {
      this._gfxID = value;
   }
   function get name()
   {
      return this._name;
   }
   function set name(value)
   {
      this._name = value;
   }
   function get Level()
   {
      return this._level;
   }
   function set Level(value)
   {
      this._level = Number(value);
      this.broadcastMessage("onSetLevel",value);
   }
   function get XP()
   {
      return this._xp;
   }
   function set XP(value)
   {
      this._xp = Number(value);
      this.broadcastMessage("onSetXP",value);
   }
   function get LP()
   {
      return this._lp;
   }
   function set LP(value)
   {
      if(this._lp == value)
      {
         return;
      }
      this._lp = Number(value) <= 0 ? 0 : Number(value);
      this.dispatchEvent({type:"lpChanged",value:value,id:this.id});
      this.broadcastMessage("onSetLP",value,this.LP);
   }
   function get LPmax()
   {
      return this._lpmax;
   }
   function set LPmax(value)
   {
      if(this._lpmax == value)
      {
         return;
      }
      this._lpmax = Number(value);
      this.dispatchEvent({type:"lpMaxChanged",value:value});
      this.broadcastMessage("onSetLPmax",this.LPmax,value);
   }
   function get AP()
   {
      return this._ap;
   }
   function set AP(value)
   {
      if(this._ap == value)
      {
         return;
      }
      this._ap = Number(value);
      this.dispatchEvent({type:"apChanged",value:value,id:this.id});
      this.broadcastMessage("onSetAP",value);
   }
   function get APinit()
   {
      return this._apinit;
   }
   function set APinit(value)
   {
      this._apinit = Number(value);
   }
   function get MP()
   {
      return this._mp;
   }
   function set MP(value)
   {
      if(this._mp == value)
      {
         return;
      }
      this._mp = Number(value);
      this.dispatchEvent({type:"mpChanged",value:value,id:this.id});
      this.broadcastMessage("onSetMP",value);
   }
   function get MPinit()
   {
      return this._mpinit;
   }
   function set MPinit(value)
   {
      this._mpinit = Number(value);
   }
   function get Kama()
   {
      return this._kama;
   }
   function set Kama(value)
   {
      this._kama = Number(value);
      this.broadcastMessage("onSetKama",value);
   }
   function get Team()
   {
      return this._team;
   }
   function set Team(value)
   {
      this._team = Number(value);
   }
   function get Weapon()
   {
      return this._aAccessories[0];
   }
   function get ToolAnimation()
   {
      var _loc2_ = this.Weapon.unicID;
      var _loc3_ = this.api.lang.getItemUnicText(_loc2_);
      if(_loc3_.an == undefined)
      {
         if(this.api.datacenter.Game.isFight)
         {
            return "anim0";
         }
         return "anim3";
      }
      return "anim" + _loc3_.an;
   }
   function get artworkFile()
   {
      return dofus.Constants.ARTWORKS_BIG_PATH + this.gfxFileName + ".swf";
   }
   function set isSummoned(bIsSummoned)
   {
      this._summoned = bIsSummoned;
   }
   function get isSummoned(bIsSummoned)
   {
      return this._summoned;
   }
   function set pvpGain(nPvpGain)
   {
      this._nPvpGain = nPvpGain;
   }
   function get pvpGain()
   {
      return this._nPvpGain;
   }
}
