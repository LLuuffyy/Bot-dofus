class dofus.graphics.gapi.ui.ItemUtility extends dofus.graphics.gapi.core.DofusAdvancedComponent
{
   var _btnClose;
   var _cbReceiptTypes;
   var _eaReceipts;
   var _lblCrafts;
   var _lblNoCrafts;
   var _lblNoReceipt;
   var _lblReceipt;
   var _lblReceiptFilter;
   var _lstCrafts;
   var _lstReceipt;
   var _oItem;
   var _winReceipt;
   var addToQueue;
   var initialized;
   var unloadThis;
   static var CLASS_NAME = "ItemUtility";
   function ItemUtility()
   {
      super();
   }
   function set item(oItem)
   {
      this._oItem = oItem;
      if(this.initialized)
      {
         this.search(oItem);
      }
   }
   function init()
   {
      super.init(false,dofus.graphics.gapi.ui.ItemUtility.CLASS_NAME);
   }
   function callClose()
   {
      this.unloadThis();
      return true;
   }
   function createChildren()
   {
      this.addToQueue({object:this,method:this.addListeners});
      this.addToQueue({object:this,method:this.initTexts});
      this.addToQueue({object:this,method:this.initData});
      this.hideCraftsViewer(true);
      this.hideReceiptViewer(true);
   }
   function addListeners()
   {
      this._btnClose.addEventListener("click",this);
      this._cbReceiptTypes.addEventListener("itemSelected",this);
   }
   function initTexts()
   {
      this._winReceipt.title = this._oItem.name;
      this._lblReceiptFilter.text = this.api.lang.getText("ITEM_TYPE");
      this._lblNoCrafts.text = this.api.lang.getText("ITEM_UTILITY_NO_CRAFTS");
      this._lblNoReceipt.text = this.api.lang.getText("ITEM_UTILITY_NO_RECEIPT");
      this._lblCrafts.text = this.api.lang.getText("ITEM_UTILITY_CRAFTS");
      this._lblReceipt.text = this.api.lang.getText("ITEM_UTILITY_RECEIPT");
   }
   function initData()
   {
      if(this._oItem != undefined)
      {
         this.search(this._oItem);
      }
   }
   function search(oItem)
   {
      this._eaReceipts = new ank.utils.ExtendedArray();
      var _loc3_ = this.api.lang.getAllCrafts();
      var _loc4_ = [];
      var _loc5_;
      var _loc6_;
      var _loc7_;
      for(var a in _loc3_)
      {
         if(a == oItem.unicID)
         {
            _loc5_ = new ank.utils.ExtendedArray();
            _loc5_.push(this.createCraftObject(Number(a),_loc3_));
            this._lstReceipt.dataProvider = _loc5_;
         }
         else
         {
            _loc6_ = _loc3_[a];
            _loc7_ = 0;
            while(_loc7_ < _loc6_.length)
            {
               if(_loc6_[_loc7_][0] == oItem.unicID)
               {
                  _loc4_.push(a);
               }
               _loc7_ = _loc7_ + 1;
            }
         }
      }
      var _loc8_ = new ank.utils.ExtendedArray();
      _loc8_.push({label:this.api.lang.getText("WITHOUT_TYPE_FILTER"),id:0});
      var _loc9_ = {};
      var _loc10_;
      var _loc11_;
      var _loc12_;
      if(_loc4_.length > 0)
      {
         _loc10_ = 0;
         while(_loc10_ < _loc4_.length)
         {
            _loc11_ = this.createCraftObject(_loc4_[_loc10_],_loc3_);
            _loc12_ = _loc11_.craftItem.type;
            if(!_loc9_[_loc12_])
            {
               _loc8_.push({label:this.api.lang.getItemTypeText(_loc12_).n,id:_loc12_});
               _loc9_[_loc12_] = true;
            }
            this._eaReceipts.push(_loc11_);
            _loc10_ = _loc10_ + 1;
         }
         this._cbReceiptTypes.dataProvider = _loc8_;
         this._cbReceiptTypes.selectedIndex = 0;
         this._lstCrafts.dataProvider = this._eaReceipts;
         this.hideCraftsViewer(false);
      }
      else
      {
         this.hideCraftsViewer(true);
      }
      this.hideReceiptViewer(this._lstReceipt.dataProvider.length != 1);
   }
   function createCraftObject(nCraftItemId, aAllCrafts)
   {
      var _loc4_ = aAllCrafts[nCraftItemId];
      var _loc5_ = {};
      _loc5_.craftItem = new dofus.datacenter.Item(0,nCraftItemId,1);
      _loc5_.items = [];
      var _loc6_ = 0;
      var _loc7_;
      var _loc8_;
      var _loc9_;
      while(_loc6_ < _loc4_.length)
      {
         _loc7_ = _loc4_[_loc6_][0];
         _loc8_ = _loc4_[_loc6_][1];
         _loc9_ = new dofus.datacenter.Item(0,_loc7_,_loc8_);
         _loc5_.items.push(_loc9_);
         _loc6_ = _loc6_ + 1;
      }
      return _loc5_;
   }
   function hideReceiptViewer(bHide)
   {
      this._lstReceipt._visible = !bHide;
      this._lblNoReceipt._visible = bHide;
   }
   function hideCraftsViewer(bHide)
   {
      this._lstCrafts._visible = !bHide;
      this._cbReceiptTypes.enabled = !bHide;
      this._lblNoCrafts._visible = bHide;
   }
   function setReceiptType(nTypeID)
   {
      var _loc3_ = new ank.utils.ExtendedArray();
      var _loc4_;
      var _loc5_;
      if(nTypeID == 0)
      {
         _loc3_ = this._eaReceipts;
      }
      else
      {
         _loc4_ = 0;
         while(_loc4_ < this._eaReceipts.length)
         {
            _loc5_ = this._eaReceipts[_loc4_];
            if(_loc5_.craftItem.type == nTypeID)
            {
               _loc3_.push(_loc5_);
            }
            _loc4_ = _loc4_ + 1;
         }
      }
      this._lstCrafts.dataProvider = _loc3_;
      var _loc6_ = this._cbReceiptTypes.dataProvider;
      var _loc7_ = 0;
      while(_loc7_ < _loc6_.length)
      {
         if(_loc6_[_loc7_].id == nTypeID)
         {
            this._cbReceiptTypes.selectedIndex = _loc7_;
            return undefined;
         }
         _loc7_ = _loc7_ + 1;
      }
   }
   function click(oEvent)
   {
      if(oEvent.target == this._btnClose)
      {
         this.callClose();
         return undefined;
      }
   }
   function itemSelected(oEvent)
   {
      var _loc0_;
      if((_loc0_ = oEvent.target._name) === "_cbReceiptTypes")
      {
         this.setReceiptType(this._cbReceiptTypes.selectedItem.id);
      }
   }
}
