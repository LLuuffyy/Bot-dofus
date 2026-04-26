class dofus.graphics.gapi.ui.temporis3.TemporisInvadeAreaItem extends ank.gapi.core.UIBasicComponent
{
   var _api;
   var _lblArea;
   var _lblTime;
   var _ldrTierInvade;
   var _mask;
   var _mcList;
   var _oItem;
   var _sword1;
   var _sword2;
   var addToQueue;
   function TemporisInvadeAreaItem()
   {
      super();
      this.initialize();
   }
   function set list(mcList)
   {
      this._mcList = mcList;
   }
   function get list()
   {
      return this._mcList;
   }
   function initialize()
   {
      this._api = _global.API;
   }
   function setValue(bUsed, sSuggested, oItem)
   {
      this._oItem = oItem;
      this.createChildren();
   }
   function init()
   {
      super.init(false);
   }
   function createChildren()
   {
      this._sword1._visible = false;
      this._sword2._visible = false;
      this.addToQueue({object:this,method:this.addListeners});
      this.addToQueue({object:this,method:this.initData});
      this.addToQueue({object:this,method:this.initTexts});
   }
   function addListeners()
   {
      var ref = this;
      this._mask.onRollOver = function()
      {
         ref.over({target:this});
      };
      this._mask.onRollOut = function()
      {
         ref.out({target:this});
      };
      this._ldrTierInvade.onRollOver = function()
      {
         ref.over({target:this});
      };
      this._ldrTierInvade.onRollOut = function()
      {
         ref.out({target:this});
      };
      this._lblArea.onRollOver = function()
      {
         ref.over({target:this});
      };
      this._lblArea.onRollOut = function()
      {
         ref.out({target:this});
      };
      this._lblArea.onPress = function()
      {
         ref._parent.select();
      };
      this._ldrTierInvade.onPress = function()
      {
         ref._parent.select();
      };
      this._mask.onPress = function()
      {
         ref._parent.select();
      };
   }
   function initData()
   {
      switch(this._oItem.completionLevel)
      {
         case 0:
            this._sword1._visible = false;
            this._sword2._visible = false;
            break;
         case 1:
         case 2:
            this._sword1._visible = true;
            this._sword2._visible = false;
            break;
         case 3:
            this._sword1._visible = true;
            this._sword2._visible = true;
      }
      switch(this._oItem.invadeLevel)
      {
         case 30:
            this._ldrTierInvade.contentPath = "clips/items/132/" + (!this._oItem.active ? "100.swf" : "9.swf");
            break;
         case 60:
            this._ldrTierInvade.contentPath = "clips/items/132/" + (!this._oItem.active ? "101.swf" : "10.swf");
            break;
         case 90:
            this._ldrTierInvade.contentPath = "clips/items/132/" + (!this._oItem.active ? "102.swf" : "11.swf");
            break;
         case 120:
            this._ldrTierInvade.contentPath = "clips/items/132/" + (!this._oItem.active ? "103.swf" : "12.swf");
            break;
         case 150:
            this._ldrTierInvade.contentPath = "clips/items/132/" + (!this._oItem.active ? "104.swf" : "13.swf");
            break;
         case 180:
            this._ldrTierInvade.contentPath = "clips/items/132/" + (!this._oItem.active ? "105.swf" : "14.swf");
            break;
         case 200:
            this._ldrTierInvade.contentPath = "clips/items/132/" + (!this._oItem.active ? "106.swf" : "15.swf");
         default:
            return;
      }
   }
   function initTexts()
   {
      this._lblTime.html = true;
      var _loc2_ = this._oItem.invadeTimer;
      if(this._oItem.active)
      {
         this._lblTime.text = this._api.lang.getText("TR3_ACTUAL_INVADE_TIME",[this._oItem.remainingTime]);
      }
      else
      {
         this._lblTime.text = this._api.lang.getText("TR3_TIME_BEFORE_INVADE",[this._oItem.remainingTime]);
      }
      this._lblArea.text = this.getAreaList();
   }
   function getAreaTooltipList()
   {
      var _loc2_ = "";
      if(!this._oItem.active)
      {
         _loc2_ += "<b>" + this._api.lang.getText("TR3_TIME_BEFORE_INVADE",[this._oItem.remainingTime]) + " :</b>\n";
      }
      else
      {
         _loc2_ += "<b>" + this._api.lang.getText("TR3_ACTUAL_INVADE_TIME",[this._oItem.remainingTime]) + " :</b>\n";
      }
      if(this._oItem.areaId[0] == "")
      {
         return _loc2_ + this._api.lang.getText("UNKNOWN_AREA");
      }
      var _loc3_ = 0;
      var _loc4_;
      while(_loc3_ < this._oItem.areaId.length)
      {
         _loc2_ += "\n - ";
         _loc4_ = Number(this._oItem.areaId[_loc3_]);
         _loc2_ += this._api.lang.getMapSubAreaName(_loc4_);
         _loc3_ = _loc3_ + 1;
      }
      return _loc2_;
   }
   function getAreaList()
   {
      var _loc2_ = "";
      if(this._oItem.areaId[0] == "")
      {
         return _loc2_ + this._api.lang.getText("UNKNOWN_AREA");
      }
      var _loc3_ = 0;
      var _loc4_;
      while(_loc3_ < this._oItem.areaId.length)
      {
         if(_loc3_ != 0)
         {
            _loc2_ += ", ";
         }
         _loc4_ = Number(this._oItem.areaId[_loc3_]);
         _loc2_ += this._api.lang.getMapSubAreaName(_loc4_);
         _loc3_ = _loc3_ + 1;
      }
      return _loc2_;
   }
   function over(oEvent)
   {
      switch(oEvent.target)
      {
         case this._mask:
            switch(this._oItem.completionLevel)
            {
               case 0:
                  this._api.ui.showTooltip(this._api.lang.getText("TR3_NOT_VALIDATED_YET_AREA"),oEvent.target,-20);
                  break;
               case 1:
                  this._api.ui.showTooltip(this._api.lang.getText("TR3_VALIDATED_AREA_ONLY_MONSTERS"),oEvent.target,-20);
                  break;
               case 2:
                  this._api.ui.showTooltip(this._api.lang.getText("TR3_VALIDATED_AREA_ONLY_BOSS"),oEvent.target,-20);
                  break;
               case 3:
                  this._api.ui.showTooltip(this._api.lang.getText("TR3_VALIDATED_AREA"),oEvent.target,-20);
            }
            break;
         case this._ldrTierInvade:
            this._api.ui.showTooltip(this._api.lang.getText("LEVEL") + " : " + this._oItem.invadeLevel,oEvent.target,-20);
            break;
         case this._lblArea:
            this._api.ui.showTooltip(this.getAreaTooltipList(),_root._xmouse + 20,_root._ymouse - 20);
         default:
            return;
      }
   }
   function out(oEvent)
   {
      this._api.ui.hideTooltip();
   }
   function click(oEvent)
   {
   }
}
