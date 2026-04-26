class dofus.graphics.gapi.ui.temporis3.TemporisDungeonItem extends ank.gapi.core.UIBasicComponent
{
   var _api;
   var _bEnabled;
   var _btnTeleport;
   var _key;
   var _lblDungeon;
   var _ldrItemKey;
   var _ldrTierInvade;
   var _maskLocked;
   var _mcCheck;
   var _mcList;
   var _oItem;
   var addToQueue;
   function TemporisDungeonItem()
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
      this._mcCheck._visible = false;
      this.addToQueue({object:this,method:this.addListeners});
      this.addToQueue({object:this,method:this.initData});
      this.addToQueue({object:this,method:this.initTexts});
   }
   function addListeners()
   {
      var ref = this;
      this._ldrTierInvade.onRollOver = function()
      {
         ref.over({target:this});
      };
      this._ldrTierInvade.onRollOut = function()
      {
         ref.out({target:this});
      };
      this._mcCheck.onRollOver = function()
      {
         ref.over({target:this});
      };
      this._mcCheck.onRollOut = function()
      {
         ref.out({target:this});
      };
      this._btnTeleport.onRollOver = function()
      {
         ref.over({target:this});
      };
      this._btnTeleport.onRollOut = function()
      {
         ref.out({target:this});
      };
      this._btnTeleport.addEventListener("click",this);
   }
   function initData()
   {
      this._bEnabled = this.hasAccess();
      switch(this._oItem.dungeonLevel)
      {
         case 30:
            this._ldrTierInvade.contentPath = dofus.Constants.ITEMS_PATH + "132/9.swf";
            break;
         case 60:
            this._ldrTierInvade.contentPath = dofus.Constants.ITEMS_PATH + "132/10.swf";
            break;
         case 90:
            this._ldrTierInvade.contentPath = dofus.Constants.ITEMS_PATH + "132/11.swf";
            break;
         case 120:
            this._ldrTierInvade.contentPath = dofus.Constants.ITEMS_PATH + "132/12.swf";
            break;
         case 150:
            this._ldrTierInvade.contentPath = dofus.Constants.ITEMS_PATH + "132/13.swf";
            break;
         case 180:
            this._ldrTierInvade.contentPath = dofus.Constants.ITEMS_PATH + "132/14.swf";
            break;
         case 200:
            this._ldrTierInvade.contentPath = dofus.Constants.ITEMS_PATH + "132/15.swf";
      }
      this._key = this.getItemInPlayerInvantory();
      this._btnTeleport.enabled = this._key != undefined && this.hasAccess();
      this._btnTeleport.selected = !this._btnTeleport.enabled;
      this._btnTeleport.icon = "clips/evenementials/temporis/3/ui/4.swf";
      this._lblDungeon.styleName = !this.hasAccess() ? "WhiteCenterSmallLabel" : "BrownCenterSmallLabel";
      this._maskLocked._visible = !this.hasAccess();
      var _loc2_ = this._api.lang.getItemUnicText(this._oItem.keyId);
      this._ldrItemKey.contentPath = dofus.Constants.ITEMS_PATH + _loc2_.t + "/" + _loc2_.g + ".swf";
      this._mcCheck._visible = this._oItem.completed;
   }
   function initTexts()
   {
      if(this.hasAccess())
      {
         this._lblDungeon.text = this._api.lang.getDungeonText(this._oItem.dungeonID).n;
      }
      else if(!this._oItem.accessible)
      {
         this._lblDungeon.text = this._api.lang.getText("NOT_YET_ACCESS");
      }
      else
      {
         this._lblDungeon.text = this._api.lang.getText("LOCKED");
      }
   }
   function getItemInPlayerInvantory()
   {
      return this._api.datacenter.Player.Inventory.findFirstItem("unicID",this._oItem.keyId).item;
   }
   function hasAccess()
   {
      return this._oItem.unlocked && this._oItem.accessible;
   }
   function over(oEvent)
   {
      switch(oEvent.target)
      {
         case this._mcCheck:
         case this._ldrTierInvade:
            this._api.ui.showTooltip(this._api.lang.getText("LEVEL") + " : " + this._oItem.dungeonLevel,this._ldrTierInvade,-20);
            break;
         case this._btnTeleport:
            this._api.ui.showTooltip("Utiliser l\'artefact pour se téléporter dans le donjon",oEvent.target,-20);
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
      if(this._key != undefined)
      {
         this._api.network.Items.use(this._key.ID);
      }
   }
}
