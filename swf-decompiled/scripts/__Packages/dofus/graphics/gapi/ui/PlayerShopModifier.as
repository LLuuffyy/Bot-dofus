class dofus.graphics.gapi.ui.PlayerShopModifier extends dofus.graphics.gapi.core.DofusAdvancedComponent
{
   var _btnAdd;
   var _btnClose;
   var _btnModify;
   var _btnOffline;
   var _btnRemove;
   var _itvItemViewer;
   var _lblQuantity;
   var _livInventory;
   var _livInventory2;
   var _mcBuyArrow;
   var _mcPrice;
   var _mcQuantity;
   var _mcSellArrow;
   var _oData;
   var _oDefaultButton;
   var _oSelectedItem;
   var _parent;
   var _sRemoveText;
   var _txtPrice;
   var _txtPriceLabel;
   var _txtQuantity;
   var _winInventory;
   var _winInventory2;
   var _winItemViewer;
   var addToQueue;
   var api;
   var gapi;
   static var CLASS_NAME = "PlayerShopModifier";
   function PlayerShopModifier()
   {
      super();
   }
   function set data(oData)
   {
      this._oData = oData;
   }
   function get currentOverItem()
   {
      if(this._livInventory != undefined && this._livInventory.currentOverItem != undefined)
      {
         return this._livInventory.currentOverItem;
      }
      if(this._livInventory2 != undefined && this._livInventory2.currentOverItem != undefined)
      {
         return this._livInventory2.currentOverItem;
      }
      return undefined;
   }
   function get itemViewer()
   {
      return this._itvItemViewer;
   }
   function init()
   {
      super.init(false,dofus.graphics.gapi.ui.PlayerShopModifier.CLASS_NAME);
   }
   function callClose()
   {
      this.gapi.hideTooltip();
      this.api.network.Exchange.leave();
      return true;
   }
   function createChildren()
   {
      this.addToQueue({object:this,method:this.addListeners});
      this.addToQueue({object:this,method:this.initData});
      this.addToQueue({object:this,method:this.initTexts});
      this.hideItemViewer(true);
      this.setAddMode(false);
      this.setModifyMode(false);
      this._txtQuantity.restrict = "0-9";
      this._txtPrice.restrict = "0-9";
      this._txtQuantity.onSetFocus = function()
      {
         this._parent.onSetFocus();
      };
      this._txtQuantity.onKillFocus = function()
      {
         this._parent.onKillFocus();
      };
      this._txtPrice.onSetFocus = function()
      {
         this._parent.onSetFocus();
      };
      this._txtPrice.onKillFocus = function()
      {
         this._parent.onKillFocus();
      };
   }
   function addListeners()
   {
      this._livInventory.addEventListener("selectedItem",this);
      this._livInventory2.addEventListener("selectedItem",this);
      this._livInventory2.addEventListener("rollOverItem",this);
      this._livInventory2.addEventListener("rollOutItem",this);
      this._livInventory2.lstInventory.multipleSelection = true;
      this._btnAdd.addEventListener("click",this);
      this._btnRemove.addEventListener("click",this);
      this._btnModify.addEventListener("click",this);
      this._btnClose.addEventListener("click",this);
      this._btnOffline.addEventListener("click",this);
      this._btnOffline.addEventListener("over",this);
      this._btnOffline.addEventListener("out",this);
      if(this._oData != undefined)
      {
         this._oData.addEventListener("modelChanged",this);
      }
      else
      {
         ank.utils.Logger.err("[PlayerShop] il n\'y a pas de data");
      }
      this.api.kernel.KeyManager.addShortcutsListener("onShortcut",this);
   }
   function initTexts()
   {
      this._btnAdd.label = this.api.lang.getText("PUT_ON_SELL");
      this.refreshRemoveButton();
      this._btnModify.label = this.api.lang.getText("MODIFY");
      this._lblQuantity.text = this.api.lang.getText("QUANTITY") + " :";
      this._txtPriceLabel.text = this.api.lang.getText("UNIT_PRICE") + " :";
      this._winInventory.title = this.api.datacenter.Player.data.name;
      this._winInventory2.title = this.api.lang.getText("SHOP_STOCK");
   }
   function initData()
   {
      this._livInventory.dataProvider = this.api.datacenter.Player.Inventory;
      this._livInventory.kamasProvider = this.api.datacenter.Player;
      this.modelChanged();
   }
   function hideItemViewer(bHide)
   {
      this._itvItemViewer._visible = !bHide;
      this._winItemViewer._visible = !bHide;
      this._mcQuantity._visible = !bHide;
      this._mcPrice._visible = !bHide;
      this._lblQuantity._visible = !bHide;
      this._txtPriceLabel._visible = !bHide;
      this._txtQuantity._visible = !bHide;
      this._txtPrice._visible = !bHide;
      if(bHide)
      {
         this._oSelectedItem = undefined;
      }
   }
   function setAddMode(bActive)
   {
      this._btnAdd._visible = bActive;
      this._mcSellArrow._visible = bActive;
      this._mcQuantity._visible = bActive;
      this._txtQuantity.editable = true;
      this._txtQuantity.selectable = true;
      this._txtPrice.tabIndex = 0;
      this._txtQuantity.tabIndex = 1;
      this._oDefaultButton = this._btnAdd;
   }
   function setModifyMode(bActive)
   {
      this._btnModify._visible = bActive;
      this._mcBuyArrow._visible = bActive;
      this._mcQuantity._visible = false;
      this._txtQuantity.editable = false;
      this._txtQuantity.selectable = false;
      this._txtPrice.tabIndex = 0;
      this._txtQuantity.tabIndex = undefined;
      this._oDefaultButton = this._btnModify;
   }
   function addToShop(oItem, nQuantity, nPrice)
   {
      this.api.network.Exchange.movementItem(true,oItem,nQuantity,nPrice);
   }
   function remove(aSelectedItems)
   {
      var _loc3_ = [];
      var _loc4_ = 0;
      var _loc5_;
      while(_loc4_ < aSelectedItems.length)
      {
         _loc5_ = aSelectedItems[_loc4_];
         _loc3_.push({Add:false,ID:_loc5_.ID,Quantity:_loc5_.Quantity});
         _loc4_ = _loc4_ + 1;
      }
      this.api.network.Exchange.movementItems(_loc3_);
   }
   function modify(oItem, nPrice)
   {
      this.api.network.Exchange.movementItem(true,oItem,0,nPrice);
   }
   function refreshRemoveButton(nTargets)
   {
      if(nTargets == undefined)
      {
         nTargets = this._livInventory2.lstInventory.getSelectedItems().length;
      }
      if(this._sRemoveText == undefined)
      {
         this._sRemoveText = this.api.lang.getText("REMOVE");
      }
      this._btnRemove.enabled = nTargets != undefined && (nTargets == 0 && (this._oSelectedItem != undefined && this._mcBuyArrow._visible) || nTargets > 0);
      if(nTargets == undefined || nTargets <= 1)
      {
         this._btnRemove.label = this._sRemoveText;
      }
      else
      {
         this._btnRemove.label = this._sRemoveText + " (" + nTargets + ")";
      }
   }
   function onShortcut(sShortcut)
   {
      if(sShortcut == "ACCEPT_CURRENT_DIALOG" && this._oSelectedItem != undefined)
      {
         this.click({target:this._oDefaultButton});
         return false;
      }
      return true;
   }
   function click(oEvent)
   {
      var _loc3_;
      var _loc4_;
      var _loc5_;
      var _loc6_;
      switch(oEvent.target._name)
      {
         case "_btnRemove":
            _loc3_ = this._livInventory2.lstInventory.getSelectedItems();
            if(!(_loc3_.length == 0 && this._oSelectedItem == undefined))
            {
               if(_loc3_.length == 0)
               {
                  _loc3_.push(this._oSelectedItem);
               }
               this.remove(_loc3_);
               this.hideItemViewer(true);
               this.setModifyMode(false);
            }
            break;
         case "_btnModify":
            _loc4_ = Number(this._txtPrice.text);
            if(_global.isNaN(_loc4_))
            {
               this.gapi.loadUIComponent("AskOk","AksOkBadPrice",{title:this.api.lang.getText("ERROR_WORD"),text:this.api.lang.getText("ERROR_INVALID_PRICE")});
            }
            else
            {
               this.modify(this._oSelectedItem,_loc4_);
               this.hideItemViewer(true);
               this.setModifyMode(false);
            }
            break;
         case "_btnAdd":
            _loc5_ = Number(this._txtPrice.text);
            _loc6_ = Number(this._txtQuantity.text);
            if(_global.isNaN(_loc5_))
            {
               this.gapi.loadUIComponent("AskOk","AksOkBadPrice",{title:this.api.lang.getText("ERROR_WORD"),text:this.api.lang.getText("ERROR_INVALID_PRICE")});
            }
            else if(_global.isNaN(_loc6_) || _loc6_ == 0)
            {
               this.gapi.loadUIComponent("AskOk","AksOkBadQuantity",{title:this.api.lang.getText("ERROR_WORD"),text:this.api.lang.getText("ERROR_INVALID_QUANTITY")});
            }
            else
            {
               _loc6_ = Math.min(this._oSelectedItem.Quantity,_loc6_);
               this.addToShop(this._oSelectedItem,_loc6_,_loc5_);
               this.hideItemViewer(true);
               this.setAddMode(false);
            }
            break;
         case "_btnClose":
            this.callClose();
            break;
         case "_btnOffline":
            this.callClose();
            this.api.kernel.GameManager.offlineExchange();
         default:
            return;
      }
   }
   function over(oEvent)
   {
      var _loc0_;
      if((_loc0_ = oEvent.target._name) === "_btnOffline")
      {
         this.gapi.showTooltip(this.api.lang.getText("MERCHANT_MODE"),oEvent.target,-20);
      }
   }
   function out(oEvent)
   {
      this.gapi.hideTooltip();
   }
   function rollOverItem(oEvent)
   {
      var _loc3_ = oEvent.targets.length;
      this.refreshRemoveButton(_loc3_);
   }
   function rollOutItem(oEvent)
   {
      var _loc3_ = oEvent.targets.length;
      this.refreshRemoveButton(_loc3_);
   }
   function selectedItem(oEvent)
   {
      var _loc3_;
      if(oEvent.item == undefined)
      {
         this.hideItemViewer(true);
         this.setAddMode(false);
         this.setModifyMode(false);
      }
      else
      {
         this._oSelectedItem = oEvent.item;
         this.hideItemViewer(false);
         this._itvItemViewer.itemData = oEvent.item;
         switch(oEvent.target._name)
         {
            case "_livInventory":
               this._txtQuantity.text = oEvent.item.Quantity;
               this._txtPrice.text = "";
               this.setModifyMode(false);
               this.setAddMode(true);
               this._livInventory2.setFilter(this._livInventory.currentFilterID);
               this.refreshRemoveButton();
               break;
            case "_livInventory2":
               _loc3_ = oEvent.targets.length;
               this._txtQuantity.text = oEvent.item.Quantity;
               this._txtPrice.text = oEvent.item.price;
               this.setAddMode(false);
               this.setModifyMode(true);
               this._livInventory.setFilter(this._livInventory2.currentFilterID);
               this.refreshRemoveButton(_loc3_);
         }
         Selection.setFocus(this._txtPrice);
      }
   }
   function modelChanged(oEvent)
   {
      this._livInventory2.dataProvider = this._oData.inventory;
   }
   function onSetFocus()
   {
      fscommand("trapallkeys","false");
   }
   function onKillFocus()
   {
      fscommand("trapallkeys","true");
   }
}
