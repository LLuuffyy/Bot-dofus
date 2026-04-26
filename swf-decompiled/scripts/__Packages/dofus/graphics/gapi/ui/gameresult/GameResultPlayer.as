class dofus.graphics.gapi.ui.gameresult.GameResultPlayer extends ank.gapi.core.UIBasicComponent
{
   var _lblGuildXP;
   var _lblKama;
   var _lblLevel;
   var _lblMountXP;
   var _lblName;
   var _lblWinXP;
   var _ldrAllDrop;
   var _ldrGuild;
   var _mcDeadHead;
   var _mcItemPlacer;
   var _mcItems;
   var _mcList;
   var _nMaxVisibleDrops;
   var _oItems;
   var _pbXP;
   var _sXP;
   var addToQueue;
   var api;
   var createEmptyMovieClip;
   var MAX_DROPS_DISPLAYED = 10;
   function GameResultPlayer()
   {
      super();
   }
   function set list(mcList)
   {
      this._mcList = mcList;
   }
   function setValue(bUsed, sSuggested, oItem)
   {
      this._oItems = oItem;
      var _loc6_;
      var _loc7_;
      var _loc8_;
      var _loc9_;
      var _loc10_;
      var _loc11_;
      var _loc12_;
      var _loc13_;
      if(bUsed)
      {
         switch(oItem.type)
         {
            case "monster":
            case "taxcollector":
            case "player":
               this._lblName.text = oItem.name;
               if(_global.isNaN(oItem.xp) || _global.isNaN(oItem.winxp))
               {
                  this._pbXP._visible = false;
               }
               else if(oItem.maxxp <= oItem.minxp || oItem.maxxp <= 0)
               {
                  this._pbXP._visible = true;
                  this._pbXP.uberMinimum = _loc0_ = 0;
                  this._pbXP.minimum = _loc0_;
                  this._pbXP.uberMaximum = _loc0_ = 100;
                  this._pbXP.maximum = _loc0_;
                  this._pbXP.value = 100;
                  this._pbXP.uberValue = 100;
               }
               else
               {
                  this._pbXP._visible = true;
                  this._pbXP.uberMinimum = _loc0_ = oItem.minxp;
                  this._pbXP.minimum = _loc0_;
                  this._pbXP.uberMaximum = _loc0_ = oItem.maxxp;
                  this._pbXP.maximum = _loc0_;
                  this._pbXP.value = oItem.xp;
                  this._pbXP.uberValue = oItem.xp - (!_global.isNaN(oItem.winxp) ? oItem.winxp : 0);
               }
               this._lblWinXP.text = !_global.isNaN(oItem.winxp) ? new ank.utils.ExtendedString(oItem.winxp).addMiddleChar(this.api.lang.getConfigText("THOUSAND_SEPARATOR"),3) : "";
               this._lblGuildXP.text = !_global.isNaN(oItem.guildxp) ? new ank.utils.ExtendedString(oItem.guildxp).addMiddleChar(this.api.lang.getConfigText("THOUSAND_SEPARATOR"),3) : "";
               this._lblMountXP.text = !_global.isNaN(oItem.mountxp) ? new ank.utils.ExtendedString(oItem.mountxp).addMiddleChar(this.api.lang.getConfigText("THOUSAND_SEPARATOR"),3) : "";
               this._lblKama.text = !_global.isNaN(oItem.kama) ? new ank.utils.ExtendedString(oItem.kama).addMiddleChar(this.api.lang.getConfigText("THOUSAND_SEPARATOR"),3) : "";
               this._lblLevel.text = oItem.level;
               if(oItem.bDead)
               {
                  this._ldrGuild.contentPath = "";
                  this._mcDeadHead._visible = true;
               }
               else
               {
                  this._ldrGuild.contentPath = dofus.Constants.GUILDS_MINI_PATH + oItem.gfx + ".swf";
                  this._mcDeadHead._visible = false;
               }
               if(_global.isNaN(this._nMaxVisibleDrops))
               {
                  this._nMaxVisibleDrops = Math.floor(this._mcItemPlacer._width / 24);
               }
               this.createEmptyMovieClip("_mcItems",10);
               _loc6_ = false;
               _loc7_ = oItem.items.length;
               _loc11_ = 0;
               while(_loc11_ < this.MAX_DROPS_DISPLAYED)
               {
                  _loc12_ = this["_ctr" + _loc11_];
                  _loc13_ = oItem.items[_loc11_];
                  _loc12_.contentData = _loc13_;
                  _loc12_._visible = _loc13_ != undefined;
                  _loc11_ += 1;
               }
               while((_loc7_ -= 1) >= 0)
               {
                  _loc8_ = this._mcItemPlacer._x + 24 * _loc7_;
                  if(_loc8_ < this._mcItemPlacer._x + this._mcItemPlacer._width)
                  {
                     _loc9_ = oItem.items[_loc7_];
                     _loc10_ = this._mcItems.attachMovie("Container","_ctrItem" + _loc7_,_loc7_,{_x:_loc8_,_y:this._mcItemPlacer._y + 1});
                     _loc10_.setSize(18,18);
                     _loc10_.addEventListener("over",this);
                     _loc10_.addEventListener("out",this);
                     _loc10_.addEventListener("click",this);
                     _loc10_.enabled = true;
                     _loc10_.margin = 0;
                     _loc10_.contentData = _loc9_;
                  }
                  else
                  {
                     _loc6_ = true;
                  }
               }
               this._ldrAllDrop._visible = _loc6_;
         }
         return undefined;
      }
      if(this._lblName.text != undefined)
      {
         this._pbXP._visible = false;
         this._lblName.text = "";
         this._pbXP.minimum = 0;
         this._pbXP.maximum = 100;
         this._pbXP.value = 0;
         this._pbXP.uberValue = 0;
         this._lblWinXP.text = "";
         this._lblKama.text = "";
         this._mcDeadHead._visible = false;
         this._mcItems.removeMovieClip();
         this._ldrAllDrop._visible = false;
      }
   }
   function init()
   {
      super.init(false);
      this._mcItemPlacer._alpha = 0;
      this._mcDeadHead._visible = false;
      this._ldrAllDrop._visible = false;
      this._nMaxVisibleDrops = Math.floor(this._mcItemPlacer._width / 24);
      this.addToQueue({object:this,method:this.addListeners});
      this.api = _global.API;
   }
   function size()
   {
      super.size();
   }
   function addListeners()
   {
      var _loc2_ = this;
      this._ldrAllDrop.addEventListener("over",this);
      this._ldrAllDrop.addEventListener("out",this);
      this._ldrAllDrop.onRollOver = function()
      {
         var _loc1_ = _loc2_._oItems.items;
         if(_loc1_ == undefined || _loc1_.length == 0)
         {
            return undefined;
         }
         var _loc2_ = "";
         var _loc3_ = 0;
         while(_loc3_ < _loc1_.length)
         {
            if(_loc3_ > 0)
            {
               _loc2_ += "\n";
            }
            _loc2_ += _loc1_[_loc3_].Quantity + " x " + _loc1_[_loc3_].name;
            _loc3_ = _loc3_ + 1;
         }
         if(_loc2_ != "")
         {
            _loc2_._mcList.gapi.showTooltip(_loc2_,_loc2_._ldrAllDrop,30);
         }
      };
      this._ldrAllDrop.onRollOut = function()
      {
         _loc2_._mcList.gapi.hideTooltip();
      };
      this._pbXP.enabled = true;
      this._pbXP.addEventListener("over",this);
      this._pbXP.addEventListener("out",this);
      var _loc2_ = 0;
      var _loc3_;
      while(_loc2_ < this.MAX_DROPS_DISPLAYED)
      {
         _loc3_ = this["_ctr" + _loc2_];
         _loc3_.addEventListener("over",this);
         _loc3_.addEventListener("out",this);
         _loc2_ += 1;
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
      switch(oEvent.target)
      {
         case this._ldrAllDrop:
            _loc3_ = this._oItems.items;
            _loc4_ = "";
            _loc5_ = 0;
            while(_loc5_ < _loc3_.length)
            {
               _loc6_ = _loc3_[_loc5_];
               if(_loc5_ > 0)
               {
                  _loc4_ += "\n";
               }
               _loc4_ += _loc6_.Quantity + " x " + _loc6_.name;
               _loc5_ += 1;
            }
            if(_loc4_ != "")
            {
               this._mcList.gapi.showTooltip(_loc4_,oEvent.target,30);
               return undefined;
            }
            return undefined;
            break;
         case this._pbXP:
            this._mcList.gapi.showTooltip(this._oItems.id != this.api.datacenter.Player.ID ? new ank.utils.ExtendedString(this._oItems.xp).addMiddleChar(this.api.lang.getConfigText("THOUSAND_SEPARATOR"),3) + " / " + new ank.utils.ExtendedString(this._oItems.maxxp > this._oItems.minxp ? this._oItems.maxxp : -1).addMiddleChar(this.api.lang.getConfigText("THOUSAND_SEPARATOR"),3) + " <b>" + this.api.lang.getText("WORD_XP") + "</b>" : this.getFormattedXPString(),oEvent.target,20);
            return undefined;
         default:
            _loc7_ = oEvent.target.contentData;
            _loc8_ = _loc7_.style + "ToolTip";
            this._mcList.gapi.showTooltip(_loc7_.Quantity + " x " + _loc7_.name,oEvent.target,20,undefined,_loc8_);
            return undefined;
      }
   }
   function out(oEvent)
   {
      this._mcList.gapi.hideTooltip();
   }
   function click(oEvent)
   {
      var _loc3_ = oEvent.target.contentData;
      if(Key.isDown(dofus.Constants.CHAT_INSERT_ITEM_KEY) && _loc3_ != undefined)
      {
         this._mcList._parent.gapi.api.kernel.GameManager.insertItemInChat(_loc3_);
      }
   }
   function getFormattedXPString()
   {
      if(this._sXP != undefined)
      {
         return this._sXP;
      }
      if(this.api.datacenter.Player.XPhigh <= this.api.datacenter.Player.XPlow)
      {
         this._sXP = this.api.lang.getText("LEVEL") + " max\n\n" + new ank.utils.ExtendedString(this._oItems.xp).addMiddleChar(this.api.lang.getConfigText("THOUSAND_SEPARATOR"),3) + " <b>" + this.api.lang.getText("WORD_XP") + "</b>";
      }
      else
      {
         this._sXP = new ank.utils.ExtendedString(this._oItems.xp).addMiddleChar(this.api.lang.getConfigText("THOUSAND_SEPARATOR"),3) + " / " + new ank.utils.ExtendedString(this._oItems.maxxp > this._oItems.minxp ? this._oItems.maxxp : -1).addMiddleChar(this.api.lang.getConfigText("THOUSAND_SEPARATOR"),3) + " <b>" + this.api.lang.getText("WORD_XP") + "</b>\n\n" + this.api.lang.getText("NEXT_LEVEL") + " " + this.api.kernel.Console.getCurrentPercent() + "\n" + this.api.lang.getText("REQUIRED") + " " + new ank.utils.ExtendedString(this.api.datacenter.Player.XPhigh - this.api.datacenter.Player.XP).addMiddleChar(" ",3);
      }
      return this._sXP;
   }
}
