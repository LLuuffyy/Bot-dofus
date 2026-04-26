class dofus.graphics.gapi.controls.InventoryViewer extends dofus.graphics.gapi.core.DofusAdvancedComponent
{
   var _aSelectedSuperTypes;
   var _btnFilterCards;
   var _btnFilterEquipement;
   var _btnFilterNonEquipement;
   var _btnFilterRessoureces;
   var _btnFilterRunes;
   var _btnFilterSoul;
   var _btnMoreChoice;
   var _btnSelectedFilterButton;
   var _cbTypes;
   var _eaDataProvider;
   var _iifFilter;
   var _lblKama;
   var _nCurrentFilterID;
   var _name;
   var _oDataViewer;
   var _oKamasProvider;
   var _sCurrentSort;
   var addToQueue;
   var gapi;
   var initialized;
   static var CLASS_NAME = "InventoryViewer";
   var _bAutoFilter = true;
   var _bFilterAtStart = true;
   var _nSelectedTypeID = 0;
   var _nLastProviderLen = 0;
   var _nLastFilterID = -1;
   function InventoryViewer()
   {
      super();
   }
   function set dataProvider(eaDataProvider)
   {
      this._eaDataProvider.removeEventListener("modelChanged",this);
      this._eaDataProvider = eaDataProvider;
      this._eaDataProvider.addEventListener("modelChanged",this);
      if(this.initialized)
      {
         this.modelChanged();
      }
   }
   function get dataProvider()
   {
      return this._eaDataProvider;
   }
   function set kamasProvider(oKamasProvider)
   {
      oKamasProvider.removeEventListener("kamaChanged",this);
      this._oKamasProvider = oKamasProvider;
      oKamasProvider.addEventListener("kamaChanged",this);
      if(this.initialized)
      {
         this.kamaChanged();
      }
   }
   function set autoFilter(bAutoFilter)
   {
      this._bAutoFilter = bAutoFilter;
   }
   function set filterAtStart(bFilterAtStart)
   {
      this._bFilterAtStart = bFilterAtStart;
   }
   function get currentFilterID()
   {
      return this._nCurrentFilterID;
   }
   function get customInventoryFilter()
   {
      return this._iifFilter;
   }
   function set customInventoryFilter(iif)
   {
      this._iifFilter = iif;
      if(this.initialized)
      {
         this.modelChanged();
      }
   }
   function get selectedItem()
   {
      return this._oDataViewer.selectedIndex;
   }
   function hideNonEquipementFilters()
   {
      this._btnMoreChoice._x = this._btnFilterNonEquipement._x;
      this._btnFilterNonEquipement._visible = false;
      this._btnFilterRessoureces._visible = false;
      this._btnFilterSoul._visible = false;
      this._btnFilterCards._visible = false;
      this._btnFilterRunes._visible = false;
      this.setFilter(0);
   }
   function hideNonCardsFilters()
   {
      this._btnMoreChoice._x = this._btnFilterNonEquipement._x;
      this._btnFilterCards._x = this._btnFilterEquipement._x;
      this._btnFilterEquipement._visible = false;
      this._btnFilterNonEquipement._visible = false;
      this._btnFilterRessoureces._visible = false;
      this._btnFilterSoul._visible = false;
      this._btnFilterRunes._visible = false;
      this.setFilter(4);
   }
   function setFilter(nFilter)
   {
      if(nFilter == this._nCurrentFilterID)
      {
         return undefined;
      }
      switch(nFilter)
      {
         case dofus.Constants.FILTER_ID_EQUIPEMENT:
            this.click({target:this._btnFilterEquipement});
            this._btnFilterEquipement.selected = true;
            return;
         case dofus.Constants.FILTER_ID_NONEQUIPEMENT:
            this.click({target:this._btnFilterNonEquipement});
            this._btnFilterNonEquipement.selected = true;
            return;
         case dofus.Constants.FILTER_ID_RESSOURCES:
            this.click({target:this._btnFilterRessoureces});
            this._btnFilterRessoureces.selected = true;
            return;
         case dofus.Constants.FILTER_ID_SOUL:
            this.click({target:this._btnFilterSoul});
            this._btnFilterSoul.selected = true;
            return;
         case dofus.Constants.FILTER_ID_CARDS:
            this.click({target:this._btnFilterCards});
            this._btnFilterCards.selected = true;
            return;
         case dofus.Constants.FILTER_ID_RUNES:
            this.click({target:this._btnFilterRunes});
            this._btnFilterRunes.selected = true;
      }
      return undefined;
   }
   function createChildren()
   {
      if(this._bFilterAtStart)
      {
         if(this._bAutoFilter)
         {
            this.addToQueue({object:this,method:this.setPreferedFilter});
         }
         else
         {
            this.addToQueue({object:this,method:this.setFilter,params:[this.getDefaultFilter()]});
         }
      }
   }
   function addListeners()
   {
      this._btnFilterEquipement.addEventListener("click",this);
      this._btnFilterNonEquipement.addEventListener("click",this);
      this._btnFilterRessoureces.addEventListener("click",this);
      this._btnFilterSoul.addEventListener("click",this);
      this._btnFilterCards.addEventListener("click",this);
      this._btnFilterRunes.addEventListener("click",this);
      this._btnMoreChoice.addEventListener("click",this);
      this._btnFilterEquipement.addEventListener("over",this);
      this._btnFilterNonEquipement.addEventListener("over",this);
      this._btnFilterRessoureces.addEventListener("over",this);
      this._btnFilterSoul.addEventListener("over",this);
      this._btnFilterCards.addEventListener("over",this);
      this._btnFilterRunes.addEventListener("over",this);
      this._btnMoreChoice.addEventListener("over",this);
      this._btnFilterEquipement.addEventListener("out",this);
      this._btnFilterNonEquipement.addEventListener("out",this);
      this._btnFilterRessoureces.addEventListener("out",this);
      this._btnFilterSoul.addEventListener("out",this);
      this._btnFilterCards.addEventListener("out",this);
      this._btnFilterRunes.addEventListener("out",this);
      this._btnMoreChoice.addEventListener("out",this);
      this._cbTypes.addEventListener("itemSelected",this);
   }
   function getDefaultFilter()
   {
      return dofus.Constants.FILTER_ID_EQUIPEMENT;
   }
   function setPreferedFilter()
   {
      var _loc2_ = [{count:0,id:dofus.Constants.FILTER_ID_EQUIPEMENT},{count:0,id:dofus.Constants.FILTER_ID_NONEQUIPEMENT},{count:0,id:dofus.Constants.FILTER_ID_RESSOURCES},{count:0,id:dofus.Constants.FILTER_ID_SOUL},{count:0,id:dofus.Constants.FILTER_ID_CARDS},{count:0,id:dofus.Constants.FILTER_ID_RUNES}];
      var _loc3_;
      for(var _loc4_ in this._eaDataProvider)
      {
         _loc3_ = this._eaDataProvider[_loc4_].superType;
         if(dofus.Constants.FILTER_EQUIPEMENT[_loc3_])
         {
            _loc2_[0].count += 1;
         }
         if(dofus.Constants.FILTER_NONEQUIPEMENT[_loc3_])
         {
            _loc2_[1].count += 1;
         }
         if(dofus.Constants.FILTER_RESSOURCES[_loc3_])
         {
            _loc2_[2].count += 1;
         }
         if(dofus.Constants.FILTER_SOUL[_loc3_])
         {
            _loc2_[3].count += 1;
         }
         if(dofus.Constants.FILTER_CARDS[_loc3_])
         {
            _loc2_[4].count += 1;
         }
         if(dofus.Constants.FILTER_RUNES[_loc3_])
         {
            _loc2_[5].count += 1;
         }
      }
      _loc2_.reverse.sortOn("count");
      this.setFilter(_loc2_[0].id);
   }
   function updateDataGrid(eaDataProvider)
   {
      var _loc3_ = this.api.datacenter.Basics[dofus.graphics.gapi.controls.InventoryViewer.CLASS_NAME + "_subfilter_" + this._btnSelectedFilterButton._name + "_" + this._name];
      this._nSelectedTypeID = _loc3_ != undefined ? _loc3_ : 0;
      var _loc4_ = new ank.utils.ExtendedArray();
      var _loc5_ = new ank.utils.ExtendedArray();
      var _loc6_ = {};
      var _loc7_;
      var _loc8_;
      var _loc9_;
      for(var _loc10_ in eaDataProvider)
      {
         _loc7_ = eaDataProvider[_loc10_];
         if(!_loc7_.isPlayerLocked)
         {
         }
         _loc8_ = _loc7_.position;
         if(_loc8_ == -1 && this._aSelectedSuperTypes[_loc7_.superType])
         {
            if(_loc7_.type == this._nSelectedTypeID || this._nSelectedTypeID == 0)
            {
               if(this._iifFilter == null || this._iifFilter == undefined || this._iifFilter.isItemListed(_loc7_))
               {
                  _loc4_.push(_loc7_);
               }
            }
            _loc9_ = _loc7_.type;
            if(_loc6_[_loc9_] != true)
            {
               _loc5_.push({label:this.api.lang.getItemTypeText(_loc9_).n,id:_loc9_});
               _loc6_[_loc9_] = true;
            }
         }
      }
      _loc5_.sortOn("label");
      _loc5_.splice(0,0,{label:this.api.lang.getText("WITHOUT_TYPE_FILTER"),id:0});
      this._cbTypes.dataProvider = _loc5_;
      this.setType(this._nSelectedTypeID);
      this._oDataViewer.dataProvider = _loc4_;
      this.sortInventory(this._sCurrentSort);
   }
   function updateData()
   {
      this.updateDataGrid(this._eaDataProvider);
   }
   function setType(nTypeID)
   {
      var _loc3_ = this._cbTypes.dataProvider;
      var _loc4_ = 0;
      while(_loc4_ < _loc3_.length)
      {
         if(_loc3_[_loc4_].id == nTypeID)
         {
            this._cbTypes.selectedIndex = _loc4_;
            return undefined;
         }
         _loc4_ += 1;
      }
      this._nSelectedTypeID = 0;
      this._cbTypes.selectedIndex = this._nSelectedTypeID;
   }
   function showSearch()
   {
      var _loc2_ = this.gapi.loadUIComponent("InventorySearch","InventorySearch",{_oDataProvider:this._oDataViewer.dataProvider},{bAlwaysOnTop:true});
      _loc2_.addEventListener("select",this);
   }
   function sortInventory(sField)
   {
      if(!sField)
      {
         return undefined;
      }
      this._oDataViewer.dataProvider.sortOn(sField,Array.NUMERIC);
      this._sCurrentSort = sField;
      this._nLastProviderLen = this._oDataViewer.dataProvider.length;
      this._nLastFilterID = this._nCurrentFilterID;
      this._oDataViewer.modelChanged();
   }
   function modelChanged()
   {
      this.updateData();
   }
   function kamaChanged(oEvent)
   {
      if(oEvent.value == undefined)
      {
         this._lblKama.text = "0";
      }
      else
      {
         this._lblKama.text = new ank.utils.ExtendedString(oEvent.value).addMiddleChar(this.api.lang.getConfigText("THOUSAND_SEPARATOR"),3);
      }
   }
   function click(oEvent)
   {
      var _loc4_;
      if(oEvent.target == this._btnMoreChoice)
      {
         _loc4_ = this.api.ui.createPopupMenu();
         _loc4_.addItem(this.api.lang.getText("INVENTORY_SEARCH"),this,this.showSearch);
         _loc4_.addItem(this.api.lang.getText("INVENTORY_DATE_SORT"),this,this.sortInventory,["_itemDateId"]);
         _loc4_.addItem(this.api.lang.getText("INVENTORY_NAME_SORT"),this,this.sortInventory,["_itemName"]);
         _loc4_.addItem(this.api.lang.getText("INVENTORY_TYPE_SORT"),this,this.sortInventory,["_itemType"]);
         _loc4_.addItem(this.api.lang.getText("INVENTORY_LEVEL_SORT"),this,this.sortInventory,["_itemLevel"]);
         _loc4_.addItem(this.api.lang.getText("INVENTORY_POD_SORT"),this,this.sortInventory,["_itemWeight"]);
         _loc4_.addItem(this.api.lang.getText("INVENTORY_QTY_SORT"),this,this.sortInventory,["_nQuantity"]);
         _loc4_.show(_root._xmouse,_root._ymouse);
         return undefined;
      }
      if(oEvent.target != this._btnSelectedFilterButton)
      {
         this._btnSelectedFilterButton.selected = false;
         this._btnSelectedFilterButton = oEvent.target;
         switch(oEvent.target._name)
         {
            case "_btnFilterEquipement":
               this._aSelectedSuperTypes = dofus.Constants.FILTER_EQUIPEMENT;
               this._nCurrentFilterID = dofus.Constants.FILTER_ID_EQUIPEMENT;
               break;
            case "_btnFilterNonEquipement":
               this._aSelectedSuperTypes = dofus.Constants.FILTER_NONEQUIPEMENT;
               this._nCurrentFilterID = dofus.Constants.FILTER_ID_NONEQUIPEMENT;
               break;
            case "_btnFilterRessoureces":
               this._aSelectedSuperTypes = dofus.Constants.FILTER_RESSOURCES;
               this._nCurrentFilterID = dofus.Constants.FILTER_ID_RESSOURCES;
               break;
            case "_btnFilterSoul":
               this._aSelectedSuperTypes = dofus.Constants.FILTER_SOUL;
               this._nCurrentFilterID = dofus.Constants.FILTER_ID_SOUL;
               break;
            case "_btnFilterCards":
               this._aSelectedSuperTypes = dofus.Constants.FILTER_CARDS;
               this._nCurrentFilterID = dofus.Constants.FILTER_ID_CARDS;
               break;
            case "_btnFilterRunes":
               this._aSelectedSuperTypes = dofus.Constants.FILTER_RUNES;
               this._nCurrentFilterID = dofus.Constants.FILTER_ID_RUNES;
         }
         this.updateData();
      }
      else
      {
         oEvent.target.selected = true;
      }
   }
   function select(oEvent)
   {
      var _loc3_ = oEvent.value;
      var _loc4_ = 0;
      while(_loc4_ < this._oDataViewer.dataProvider.length)
      {
         if(_loc3_ == this._oDataViewer.dataProvider[_loc4_].unicID)
         {
            this._oDataViewer.setVPosition(Math.floor(_loc4_ / this._oDataViewer.visibleColumnCount));
            this._oDataViewer.selectedIndex = _loc4_;
         }
         _loc4_ += 1;
      }
   }
   function itemSelected(oEvent)
   {
      var _loc3_;
      if((_loc3_ = oEvent.target._name) === "_cbTypes")
      {
         this._nSelectedTypeID = this._cbTypes.selectedItem.id;
         this.api.datacenter.Basics[dofus.graphics.gapi.controls.InventoryViewer.CLASS_NAME + "_subfilter_" + this._btnSelectedFilterButton._name + "_" + this._name] = this._nSelectedTypeID;
         this.updateData();
      }
   }
   function over(oEvent)
   {
      switch(oEvent.target)
      {
         case this._btnFilterEquipement:
            this.api.ui.showTooltip(this.api.lang.getText("EQUIPEMENT"),oEvent.target,-20);
            return;
         case this._btnFilterNonEquipement:
            this.api.ui.showTooltip(this.api.lang.getText("NONEQUIPEMENT"),oEvent.target,-20);
            return;
         case this._btnFilterRessoureces:
            this.api.ui.showTooltip(this.api.lang.getText("RESSOURECES"),oEvent.target,-20);
            return;
         case this._btnFilterSoul:
            this.api.ui.showTooltip(this.api.lang.getText("SOUL"),oEvent.target,-20);
            return;
         case this._btnFilterCards:
            this.api.ui.showTooltip(this.api.lang.getText("CARDS"),oEvent.target,-20);
            return;
         case this._btnFilterRunes:
            this.api.ui.showTooltip(this.api.lang.getText("RUNES"),oEvent.target,-20);
            return;
         case this._btnMoreChoice:
            this.api.ui.showTooltip(this.api.lang.getText("SEARCH_AND_SORT"),oEvent.target,-20);
      }
      return undefined;
   }
   function out(oEvent)
   {
      this.api.ui.hideTooltip();
   }
}
