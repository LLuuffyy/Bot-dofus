class dofus.graphics.gapi.ui.SecureCraft extends dofus.graphics.gapi.core.DofusAdvancedComponent
{
   var _aGarbageMemory;
   var _bFiligraneVisible;
   var _bPayMode;
   var _btnClose;
   var _btnCraft;
   var _btnFilterCards;
   var _btnFilterEquipement;
   var _btnFilterNonEquipement;
   var _btnFilterRessoureces;
   var _btnFilterRunes;
   var _btnFilterSoul;
   var _btnPay;
   var _btnPayIfSuccessKama;
   var _btnPayKama;
   var _btnPrivateMessage;
   var _btnPrivateMessagePay;
   var _btnSelectedFilterButton;
   var _btnValidate;
   var _btnValidatePay;
   var _cbTypes;
   var _cgCoop;
   var _cgDistant;
   var _cgGrid;
   var _cgLocal;
   var _cgPay;
   var _cgPayIfSuccess;
   var _ctrPreview;
   var _currentOverContainer;
   var _cvCraftViewer;
   var _eaCoopDataProvider;
   var _eaDataProvider;
   var _eaDistantDataProvider;
   var _eaLocalDataProvider;
   var _eaPayDataProvider;
   var _eaPayIfSuccessDataProvider;
   var _eaReadyDataProvider;
   var _itvItemViewer;
   var _lblFilter;
   var _lblKama;
   var _lblNewObject;
   var _lblPay;
   var _lblPayIfSuccess;
   var _lblPayIfSuccessKama;
   var _lblPayKama;
   var _lblSkill;
   var _mcArrow;
   var _mcBlinkPay;
   var _mcBlinkPayIfSuccess;
   var _mcBlinkPayIfSuccess_TripleFramerate;
   var _mcBlinkPay_TripleFramerate;
   var _mcFiligrane;
   var _mcPayHighlight;
   var _mcPayIfSuccessHighlight;
   var _mcPayIfSuccessKama;
   var _mcPayKama;
   var _mcPlacer;
   var _nForgemagusItemType;
   var _nKamaPayment;
   var _nKamaPaymentIfSuccess;
   var _nMaxItem;
   var _nPayBar;
   var _nSkillId;
   var _parent;
   var _sCurrentDragSource;
   var _sCurrentDragTarget;
   var _winCoop;
   var _winCraftViewer;
   var _winDistant;
   var _winInventory;
   var _winItemViewer;
   var _winLocal;
   var _winPay;
   var addToQueue;
   var attachMovie;
   var gapi;
   var getNextHighestDepth;
   var setMovieClipTransform;
   static var CLASS_NAME = "SecureCraft";
   static var READY_COLOR = {ra:70,rb:0,ga:70,gb:0,ba:70,bb:0};
   static var NON_READY_COLOR = {ra:100,rb:0,ga:100,gb:0,ba:100,bb:0};
   static var GRID_CONTAINER_WIDTH = 33;
   static var DELAY_BEFORE_VALIDATE = 3000;
   static var FILTER_TYPE_ONLY_USEFUL = 10000;
   var _bInvalidateCoop = false;
   var _aSelectedSuperTypes = dofus.Constants.FILTER_RESSOURCES;
   var _nSelectedTypeID = 0;
   static var secureCraftNotified = false;
   var _nLastRegenerateTimer = 0;
   static var NAME_GENERATION_DELAY = 1000;
   function SecureCraft()
   {
      super();
   }
   function get currentOverItem()
   {
      if(this._currentOverContainer != undefined && this._currentOverContainer.contentData != undefined)
      {
         return dofus.datacenter.Item(this._currentOverContainer.contentData);
      }
      return undefined;
   }
   function get itemViewer()
   {
      return this._itvItemViewer;
   }
   function set maxItem(nMaxItem)
   {
      this._nMaxItem = Number(nMaxItem);
   }
   function set skillId(nSkillId)
   {
      this._nSkillId = Number(nSkillId);
      this._nForgemagusItemType = _global.API.lang.getSkillForgemagus(this._nSkillId);
   }
   function get isClient()
   {
      return this.api.datacenter.Basics.aks_exchange_echangeType == 13;
   }
   function set dataProvider(eaDataProvider)
   {
      this._eaDataProvider.removeEventListener("modelChange",this);
      this._eaDataProvider = eaDataProvider;
      this._eaDataProvider.addEventListener("modelChanged",this);
      this.modelChanged({target:this._eaDataProvider});
   }
   function set localDataProvider(eaLocalDataProvider)
   {
      this._eaLocalDataProvider.removeEventListener("modelChange",this);
      this._eaLocalDataProvider = eaLocalDataProvider;
      this._eaLocalDataProvider.addEventListener("modelChanged",this);
      this.modelChanged({target:this._eaLocalDataProvider});
   }
   function set distantDataProvider(eaDistantDataProvider)
   {
      this._eaDistantDataProvider.removeEventListener("modelChange",this);
      this._eaDistantDataProvider = eaDistantDataProvider;
      this._eaDistantDataProvider.addEventListener("modelChanged",this);
      this.modelChanged({target:this._eaDistantDataProvider});
   }
   function set coopDataProvider(eaCoopDataProvider)
   {
      this._eaCoopDataProvider.removeEventListener("modelChange",this);
      this._eaCoopDataProvider = eaCoopDataProvider;
      this._eaCoopDataProvider.addEventListener("modelChanged",this);
      this.modelChanged({target:this._eaCoopDataProvider});
   }
   function set payDataProvider(eaPayDataProvider)
   {
      this._eaPayDataProvider.removeEventListener("modelChange",this);
      this._eaPayDataProvider = eaPayDataProvider;
      this._eaPayDataProvider.addEventListener("modelChanged",this);
      this.modelChanged({target:this._eaPayDataProvider});
   }
   function set payIfSuccessDataProvider(eaPayIfSuccessDataProvider)
   {
      this._eaPayIfSuccessDataProvider.removeEventListener("modelChange",this);
      this._eaPayIfSuccessDataProvider = eaPayIfSuccessDataProvider;
      this._eaPayIfSuccessDataProvider.addEventListener("modelChanged",this);
      this.modelChanged({target:this._eaPayIfSuccessDataProvider});
   }
   function set readyDataProvider(eaReadyDataProvider)
   {
      this._eaReadyDataProvider.removeEventListener("modelChange",this);
      this._eaReadyDataProvider = eaReadyDataProvider;
      this._eaReadyDataProvider.addEventListener("modelChanged",this);
      this.modelChanged();
   }
   function init()
   {
      super.init(false,dofus.graphics.gapi.ui.SecureCraft.CLASS_NAME);
   }
   function destroy()
   {
      this.gapi.hideTooltip();
   }
   function callClose()
   {
      this.api.network.Exchange.leave();
      return true;
   }
   function createChildren()
   {
      this._mcPlacer._visible = false;
      this._winCraftViewer.swapDepths(this.getNextHighestDepth());
      this.showPreview(undefined,false);
      this.showCraftViewer(false);
      this.addToQueue({object:this,method:this.addListeners});
      this._btnSelectedFilterButton = this._btnFilterRessoureces;
      this.addToQueue({object:this,method:this.saveGridMaxSize});
      this.addToQueue({object:this,method:this.initData});
      this.hideItemViewer(true);
      this.addToQueue({object:this,method:this.initTexts});
      this.addToQueue({object:this,method:this.initGridWidth});
      this.api.datacenter.Player.addEventListener("kamaChanged",this);
   }
   function addListeners()
   {
      this._cgGrid.addEventListener("dblClickItem",this);
      this._cgGrid.addEventListener("dropItem",this);
      this._cgGrid.addEventListener("dragItem",this);
      this._cgGrid.addEventListener("selectItem",this);
      this._cgGrid.addEventListener("overItem",this);
      this._cgGrid.addEventListener("outItem",this);
      this._cgLocal.addEventListener("dblClickItem",this);
      this._cgLocal.addEventListener("dropItem",this);
      this._cgLocal.addEventListener("dragItem",this);
      this._cgLocal.addEventListener("selectItem",this);
      this._cgLocal.addEventListener("overItem",this);
      this._cgLocal.addEventListener("outItem",this);
      this._cgDistant.addEventListener("selectItem",this);
      this._cgDistant.addEventListener("overItem",this);
      this._cgDistant.addEventListener("outItem",this);
      this._cgCoop.addEventListener("selectItem",this);
      this._cgCoop.addEventListener("overItem",this);
      this._cgCoop.addEventListener("outItem",this);
      this._btnFilterEquipement.addEventListener("click",this);
      this._btnFilterNonEquipement.addEventListener("click",this);
      this._btnFilterRessoureces.addEventListener("click",this);
      this._btnFilterSoul.addEventListener("click",this);
      this._btnFilterCards.addEventListener("click",this);
      this._btnFilterEquipement.addEventListener("over",this);
      this._btnFilterNonEquipement.addEventListener("over",this);
      this._btnFilterRessoureces.addEventListener("over",this);
      this._btnFilterSoul.addEventListener("over",this);
      this._btnFilterCards.addEventListener("over",this);
      this._btnFilterEquipement.addEventListener("out",this);
      this._btnFilterNonEquipement.addEventListener("out",this);
      this._btnFilterRessoureces.addEventListener("out",this);
      this._btnFilterRunes.addEventListener("out",this);
      this._btnFilterRunes.addEventListener("click",this);
      this._btnFilterRunes.addEventListener("over",this);
      this._btnClose.addEventListener("click",this);
      this.api.datacenter.Exchange.addEventListener("localKamaChange",this);
      this.api.datacenter.Exchange.addEventListener("distantKamaChange",this);
      this.api.datacenter.Exchange.addEventListener("payKamaChange",this);
      this.api.datacenter.Exchange.addEventListener("payIfSuccessKamaChange",this);
      this._btnValidate.addEventListener("click",this);
      this._btnCraft.addEventListener("click",this);
      this._btnPrivateMessage.addEventListener("click",this);
      this._btnPay.addEventListener("click",this);
      this._mcFiligrane.onRollOver = function()
      {
         this._parent.over({target:this});
      };
      this._mcFiligrane.onRollOut = function()
      {
         this._parent.out({target:this});
      };
      this._cbTypes.addEventListener("itemSelected",this);
      this._cgPay.addEventListener("selectItem",this);
      this._cgPayIfSuccess.addEventListener("selectItem",this);
      this._btnPrivateMessagePay.addEventListener("click",this);
      this._btnValidatePay.addEventListener("click",this);
      if(this.isClient)
      {
         this._cgPay.addEventListener("dblClickItem",this);
         this._cgPay.addEventListener("dropItem",this);
         this._cgPayIfSuccess.addEventListener("dblClickItem",this);
         this._cgPayIfSuccess.addEventListener("dropItem",this);
      }
      this._mcPayIfSuccessHighlight.onRelease = function()
      {
         this._parent.switchPayBar(2);
      };
      this._mcPayHighlight.onRelease = function()
      {
         this._parent.switchPayBar(1);
      };
      this._cgCoop.multipleContainerSelectionEnabled = false;
      this._cgDistant.multipleContainerSelectionEnabled = false;
      this._cgGrid.multipleContainerSelectionEnabled = false;
      this._cgLocal.multipleContainerSelectionEnabled = false;
      this._cgPay.multipleContainerSelectionEnabled = false;
      this._cgPayIfSuccess.multipleContainerSelectionEnabled = false;
   }
   function initTexts()
   {
      this._winInventory.title = this.api.datacenter.Player.data.name;
      this._winDistant.title = this.api.datacenter.Sprites.getItemAt(this.api.datacenter.Exchange.distantPlayerID).name;
      this._btnValidate.label = this.api.lang.getText("COMBINE");
      this._btnValidatePay.label = this.api.lang.getText("VALIDATE");
      this._btnCraft.label = this.api.lang.getText("RECEIPTS");
      this._btnPrivateMessage.label = this.api.lang.getText("WISPER_MESSAGE");
      this._btnPrivateMessagePay.label = this.api.lang.getText("WISPER_MESSAGE");
      this._btnPay.label = this.api.lang.getText("PAY");
      this._lblNewObject.text = this.api.lang.getText("CRAFTED_ITEM");
      this._winCraftViewer.title = this.api.lang.getText("RECEIPTS_FROM_JOB");
      this._lblSkill.text = this.api.lang.getText("SKILL") + " : " + this.api.lang.getSkillText(this._nSkillId).d;
      this._winDistant.title = this.api.datacenter.Sprites.getItemAt(this.api.datacenter.Exchange.distantPlayerID).name;
      this._lblKama.text = new ank.utils.ExtendedString(this.api.datacenter.Player.Kama).addMiddleChar(this.api.lang.getConfigText("THOUSAND_SEPARATOR"),3);
      this._mcPayKama._visible = this.isClient;
      this._mcPayIfSuccessKama._visible = this.isClient;
      this._lblPay.text = this.api.lang.getText("PAY");
      this._lblPayIfSuccess.text = this.api.lang.getText("GRANT_IF_SUCCESS");
   }
   function initData()
   {
      this.dataProvider = this.api.datacenter.Exchange.inventory;
      this.localDataProvider = this.api.datacenter.Exchange.localGarbage;
      this.distantDataProvider = this.api.datacenter.Exchange.distantGarbage;
      this.coopDataProvider = this.api.datacenter.Exchange.coopGarbage;
      this.payDataProvider = this.api.datacenter.Exchange.payGarbage;
      this.payIfSuccessDataProvider = this.api.datacenter.Exchange.payIfSuccessGarbage;
      this.readyDataProvider = this.api.datacenter.Exchange.readyStates;
      this.switchToPayMode(false);
      this.switchPayBar(1);
      this.showPreview(undefined,false);
   }
   function updateInventory()
   {
      this.api.datacenter.Exchange.inventory = this.api.datacenter.Player.Inventory.deepClone();
      this.dataProvider = this.api.datacenter.Exchange.inventory;
      this.switchToPayMode(false);
   }
   function saveGridMaxSize()
   {
   }
   function initGridWidth()
   {
      if(this._nMaxItem == undefined)
      {
         this._nMaxItem = 9;
      }
      this._cgLocal.visibleColumnCount = this._nMaxItem;
      this._cgDistant.visibleColumnCount = this._nMaxItem;
      var _loc2_ = dofus.graphics.gapi.ui.SecureCraft.GRID_CONTAINER_WIDTH * this._nMaxItem;
      this._cgLocal.setSize(_loc2_);
      this._cgLocal._x = this._winLocal._x + this._winLocal.width - _loc2_ - 10;
      this._cgDistant.setSize(_loc2_);
      this._cgDistant._x = this._winDistant._x + 10;
   }
   function updateData()
   {
      var _loc3_ = this.api.datacenter.Basics[dofus.graphics.gapi.ui.SecureCraft.CLASS_NAME + "_subfilter_" + this._btnSelectedFilterButton._name];
      this._nSelectedTypeID = _loc3_ != undefined ? _loc3_ : 0;
      var _loc4_ = new ank.utils.ExtendedArray();
      var _loc5_ = new ank.utils.ExtendedArray();
      var _loc6_ = {};
      var _loc7_;
      var _loc8_;
      var _loc9_;
      var _loc10_;
      for(var _loc11_ in this._eaDataProvider)
      {
         _loc7_ = this._eaDataProvider[_loc11_];
         _loc8_ = _loc7_.position;
         if(_loc7_.isPlayerLocked)
         {
            _global.trace("[PlayerLock] Filtering locked item " + _loc7_.ID + " from SecureCraft");
         }
         if(_loc8_ == -1 && this._aSelectedSuperTypes[_loc7_.superType])
         {
            if(_loc7_.type == this._nSelectedTypeID || this._nSelectedTypeID == 0)
            {
               _loc9_ = 0;
               if(this._sCurrentDragTarget == "_cgPay")
               {
                  _loc9_ = this.getQtyIn(this._eaPayIfSuccessDataProvider,_loc7_.unicID);
               }
               else if(this._sCurrentDragTarget == "_cgPayIfSuccess")
               {
                  _loc9_ = this.getQtyIn(this._eaPayDataProvider,_loc7_.unicID);
               }
               else if(this._sCurrentDragTarget == "_cgGrid")
               {
                  if(this._sCurrentDragSource == "_cgPay")
                  {
                     _loc9_ = this.getQtyIn(this._eaPayIfSuccessDataProvider,_loc7_.unicID);
                  }
                  else if(this._sCurrentDragSource == "_cgPayIfSuccess")
                  {
                     _loc9_ = this.getQtyIn(this._eaPayDataProvider,_loc7_.unicID);
                  }
               }
               _loc7_.Quantity -= _loc9_;
               _loc4_.push(_loc7_);
            }
            else if(this._nSelectedTypeID == dofus.graphics.gapi.ui.SecureCraft.FILTER_TYPE_ONLY_USEFUL && this.api.kernel.GameManager.isItemUseful(_loc7_.unicID,this._nSkillId,this._nMaxItem))
            {
               _loc4_.push(_loc7_);
            }
            _loc10_ = _loc7_.type;
            if(_loc6_[_loc10_] != true)
            {
               _loc5_.push({label:this.api.lang.getItemTypeText(_loc10_).n,id:_loc10_});
               _loc6_[_loc10_] = true;
            }
         }
      }
      _loc5_.sortOn("label");
      _loc5_.splice(0,0,{label:this.api.lang.getText("TYPE_FILTER_ONLY_USEFUL"),id:dofus.graphics.gapi.ui.SecureCraft.FILTER_TYPE_ONLY_USEFUL});
      _loc5_.splice(0,0,{label:this.api.lang.getText("WITHOUT_TYPE_FILTER"),id:0});
      this._cbTypes.dataProvider = _loc5_;
      this.setType(this._nSelectedTypeID);
      this._cgGrid.dataProvider = _loc4_;
   }
   function getQtyIn(eaFrom, nItemID)
   {
      for(var _loc3_ in eaFrom)
      {
         if(eaFrom[_loc3_].unicID == nItemID)
         {
            return eaFrom[_loc3_].Quantity;
         }
      }
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
   function updateLocalData()
   {
      this._cgLocal.dataProvider = this._eaLocalDataProvider;
      this._bInvalidateCoop = true;
      this.hideButtonValidate(true);
      ank.utils.Timer.setTimer(this,"securecraft",this,this.hideButtonValidate,dofus.graphics.gapi.ui.SecureCraft.DELAY_BEFORE_VALIDATE,[false]);
   }
   function updateCoopData()
   {
      this._cgCoop.dataProvider = this._eaCoopDataProvider;
      this._mcFiligrane._visible = this._bFiligraneVisible = this._eaCoopDataProvider == undefined;
      var _loc2_ = dofus.datacenter.Item(this._cgCoop.getContainer(0).contentData);
      if(_loc2_ != undefined)
      {
         this.hideItemViewer(false);
         this._itvItemViewer.itemData = _loc2_;
      }
   }
   function updatePayData(bShow)
   {
      this._cgPay.dataProvider = this._eaPayDataProvider;
      if(!bShow)
      {
         return undefined;
      }
      this.switchToPayMode(true);
      this.hideButtonValidate(true);
      ank.utils.Timer.setTimer(this,"securecraft",this,this.hideButtonValidate,dofus.graphics.gapi.ui.SecureCraft.DELAY_BEFORE_VALIDATE,[false]);
   }
   function updatePayIfSuccessData(bShow)
   {
      this._cgPayIfSuccess.dataProvider = this._eaPayIfSuccessDataProvider;
      if(!bShow)
      {
         return undefined;
      }
      this.switchToPayMode(true);
      this.hideButtonValidate(true);
      ank.utils.Timer.setTimer(this,"securecraft",this,this.hideButtonValidate,dofus.graphics.gapi.ui.SecureCraft.DELAY_BEFORE_VALIDATE,[false]);
   }
   function updateDistantData()
   {
      this._cgDistant.dataProvider = this._eaDistantDataProvider;
      this._bInvalidateCoop = true;
      this.hideButtonValidate(true);
      ank.utils.Timer.setTimer(this,"securecraft",this,this.hideButtonValidate,dofus.graphics.gapi.ui.SecureCraft.DELAY_BEFORE_VALIDATE,[false]);
   }
   function updateReadyState()
   {
      var _loc2_ = !this._eaReadyDataProvider[0] ? dofus.graphics.gapi.ui.SecureCraft.NON_READY_COLOR : dofus.graphics.gapi.ui.SecureCraft.READY_COLOR;
      this.setMovieClipTransform(this._winLocal,_loc2_);
      this.setMovieClipTransform(this._btnValidate,_loc2_);
      this.setMovieClipTransform(this._cgLocal,_loc2_);
      _loc2_ = !this._eaReadyDataProvider[1] ? dofus.graphics.gapi.ui.SecureCraft.NON_READY_COLOR : dofus.graphics.gapi.ui.SecureCraft.READY_COLOR;
      this.setMovieClipTransform(this._winDistant,_loc2_);
      this.setMovieClipTransform(this._cgDistant,_loc2_);
   }
   function hideButtonValidate(bHide)
   {
      var _loc3_ = !bHide ? dofus.graphics.gapi.ui.SecureCraft.NON_READY_COLOR : dofus.graphics.gapi.ui.SecureCraft.READY_COLOR;
      this.setMovieClipTransform(this._btnValidate,_loc3_);
      this._btnValidate.enabled = !bHide;
   }
   function hideItemViewer(bHide)
   {
      this._itvItemViewer._visible = !bHide;
      this._winItemViewer._visible = !bHide;
   }
   function validateDrop(sToGrid, oItem, nValue)
   {
      if(nValue < 1 || nValue == undefined)
      {
         return undefined;
      }
      if(nValue > oItem.Quantity)
      {
         nValue = oItem.Quantity;
      }
      this._sCurrentDragTarget = sToGrid;
      switch(sToGrid)
      {
         case "_cgGrid":
            if(!this._bPayMode)
            {
               this.api.network.Exchange.movementItem(false,oItem,nValue);
               break;
            }
            this.api.network.Exchange.movementPayItem(this._nPayBar,false,oItem.ID,nValue);
            break;
         case "_cgLocal":
            this.api.network.Exchange.movementItem(true,oItem,nValue);
            break;
         case "_cgPay":
            this.api.network.Exchange.movementPayItem(1,true,oItem.ID,nValue);
            break;
         case "_cgPayIfSuccess":
            this.api.network.Exchange.movementPayItem(2,true,oItem.ID,nValue);
      }
      if(this._bInvalidateCoop)
      {
         this.api.datacenter.Exchange.clearCoopGarbage();
         this._bInvalidateCoop = false;
      }
   }
   function setReady()
   {
      var _loc2_ = this.getTotalCraftInventory();
      if(_loc2_.length == 0)
      {
         return undefined;
      }
      if(_loc2_.length > this._nMaxItem)
      {
         this.api.kernel.showMessage(undefined,this.api.lang.getText("NOT_ENOUGHT_CRAFT_SLOT",[this._nMaxItem]),"ERROR_BOX",{name:"NotEnoughtCraftSlot"});
         return undefined;
      }
      this.api.network.Exchange.ready();
   }
   function canDropInGarbage(oItem)
   {
      var _loc3_ = this.api.datacenter.Exchange.localGarbage.findFirstItem("ID",oItem.ID);
      var _loc4_ = this.api.datacenter.Exchange.localGarbage.length;
      if(_loc3_.index == -1 && _loc4_ >= this._nMaxItem)
      {
         return false;
      }
      return true;
   }
   function showCraftViewer(bShow)
   {
      var _loc3_;
      if(bShow)
      {
         _loc3_ = this.attachMovie("CraftViewer","_cvCraftViewer",this.getNextHighestDepth());
         _loc3_._x = this._mcPlacer._x;
         _loc3_._y = this._mcPlacer._y;
         _loc3_.skill = new dofus.datacenter.Skill(this._nSkillId,this._nMaxItem);
      }
      else
      {
         this._cvCraftViewer.removeMovieClip();
      }
      this._winCraftViewer._visible = bShow;
   }
   function recordGarbage()
   {
      this._aGarbageMemory = [];
      var _loc2_ = 0;
      var _loc3_;
      while(_loc2_ < this._eaLocalDataProvider.length)
      {
         _loc3_ = this._eaLocalDataProvider[_loc2_];
         this._aGarbageMemory.push({id:_loc3_.ID,quantity:_loc3_.Quantity});
         _loc2_ += 1;
      }
   }
   function cleanGarbage()
   {
      var _loc2_ = 0;
      var _loc3_;
      while(_loc2_ < this._eaLocalDataProvider.length)
      {
         _loc3_ = this._eaLocalDataProvider[_loc2_];
         this.api.network.Exchange.movementItem(false,_loc3_,_loc3_.Quantity);
         _loc2_ += 1;
      }
   }
   function recallGarbageMemory()
   {
      if(this._aGarbageMemory == undefined || this._aGarbageMemory.length == 0)
      {
         return undefined;
      }
      this.cleanGarbage();
      var _loc2_ = 0;
      var _loc3_;
      var _loc4_;
      while(_loc2_ < this._aGarbageMemory.length)
      {
         _loc3_ = this._aGarbageMemory[_loc2_];
         _loc4_ = this._eaDataProvider.findFirstItem("ID",_loc3_.id);
         if(_loc4_.index != -1)
         {
            if(_loc4_.item.Quantity < _loc3_.quantity)
            {
               this.api.kernel.showMessage(undefined,this.api.lang.getText("CRAFT_NOT_ENOUGHT",[_loc4_.item.name]),"ERROR_BOX",{name:"NotEnougth"});
               return undefined;
            }
            this.api.network.Exchange.movementItem(true,_loc4_.item,_loc3_.quantity);
         }
         else
         {
            this.api.kernel.showMessage(undefined,this.api.lang.getText("CRAFT_NO_RESOURCE"),"ERROR_BOX",{name:"NotEnougth"});
         }
         _loc2_ += 1;
      }
   }
   function showPreview(item, b)
   {
      if(this._ctrPreview.contentPath == undefined)
      {
         return undefined;
      }
      this._mcFiligrane._visible = this._bFiligraneVisible = b;
      this._ctrPreview._visible = b;
      this._ctrPreview.contentPath = !b ? "" : item.iconFile;
      this._mcFiligrane.itemName = item.name;
      if(_global.API.datacenter.Basics.aks_current_server.isTemporis())
      {
         this._ctrPreview.filters = undefined;
         if(item.realUnicId >= dofus.Constants.REFFINED_ITEM.minimumID)
         {
            item.addGlowOnItemIcon(this._ctrPreview,dofus.Constants.REFFINED_ITEM.color,dofus.Constants.REFFINED_ITEM.alpha,dofus.Constants.REFFINED_ITEM.blur,dofus.Constants.REFFINED_ITEM.intensity);
         }
         else if(item.realUnicId >= dofus.Constants.IMPROVED_ITEM.minimumID)
         {
            item.addGlowOnItemIcon(this._ctrPreview,dofus.Constants.IMPROVED_ITEM.color,dofus.Constants.IMPROVED_ITEM.alpha,dofus.Constants.IMPROVED_ITEM.blur,dofus.Constants.IMPROVED_ITEM.intensity);
         }
      }
   }
   function updatePreview()
   {
      var _loc2_ = this.api.kernel.GameManager.analyseReceipts(this.getTotalCraftInventory(),this._nSkillId,this._nMaxItem);
      if(_loc2_ != undefined)
      {
         this.showPreview(new dofus.datacenter.Item(-1,_loc2_,1,0,"",0),true);
      }
      else
      {
         this.showPreview(undefined,false);
      }
   }
   function getTotalCraftInventory()
   {
      var _loc2_ = this.api.kernel.GameManager;
      return _loc2_.mergeUnicItemInInventory(_loc2_.mergeTwoInventory(this._eaLocalDataProvider,this._eaDistantDataProvider));
   }
   function switchToPayMode(bShow)
   {
      if(bShow == undefined && this._bPayMode == undefined)
      {
         return undefined;
      }
      if(bShow == undefined)
      {
         this._bPayMode = !this._bPayMode;
      }
      else
      {
         if(this._bPayMode == bShow)
         {
            return undefined;
         }
         this._bPayMode = bShow;
      }
      this.gapi.removeCursor();
      this._winLocal._visible = !this._bPayMode;
      this._cgLocal._visible = !this._bPayMode;
      this._btnPrivateMessage._visible = !this._bPayMode;
      this._winDistant._visible = !this._bPayMode;
      this._cgDistant._visible = !this._bPayMode;
      this._btnPay._visible = !this._bPayMode;
      this._winCoop._visible = !this._bPayMode;
      this._lblNewObject._visible = !this._bPayMode;
      this._mcFiligrane._visible = !this._bPayMode ? this._bFiligraneVisible : false;
      this._ctrPreview._visible = !this._bPayMode ? this._bFiligraneVisible : false;
      this._cgCoop._visible = !this._bPayMode;
      this._btnCraft._visible = !this._bPayMode;
      this._btnValidate._visible = !this._bPayMode;
      this._mcArrow._visible = !this._bPayMode;
      this._winPay._visible = this._bPayMode;
      this._btnPrivateMessagePay._visible = this._bPayMode;
      this._btnValidatePay._visible = this._bPayMode;
      this._winItemViewer._y = !this._bPayMode ? 30 : 57;
      this._itvItemViewer._y = !this._bPayMode ? 30 : 57;
      this._mcBlinkPay._visible = this._bPayMode;
      this._mcBlinkPay_TripleFramerate._visible = this._bPayMode;
      this._cgPay._visible = this._bPayMode;
      this._lblPayKama._visible = this._bPayMode;
      this._mcPayKama._visible = this._bPayMode;
      this._lblPay._visible = this._bPayMode;
      this._btnPayKama._visible = this._bPayMode && this.isClient;
      this._mcBlinkPayIfSuccess._visible = this._bPayMode;
      this._mcBlinkPayIfSuccess_TripleFramerate._visible = this._bPayMode;
      this._cgPayIfSuccess._visible = this._bPayMode;
      this._lblPayIfSuccessKama._visible = this._bPayMode;
      this._mcPayIfSuccessKama._visible = this._bPayMode;
      this._lblPayIfSuccess._visible = this._bPayMode;
      this._btnPayIfSuccessKama._visible = this._bPayMode && this.isClient;
      this.switchPayBar();
   }
   function switchPayBar(nPayBar)
   {
      if(nPayBar != undefined)
      {
         this._nPayBar = nPayBar;
      }
      this._mcPayHighlight._visible = this._bPayMode && this.isClient;
      this._mcPayIfSuccessHighlight._visible = this._bPayMode && this.isClient;
      this._mcPayHighlight._alpha = this._nPayBar != 1 ? 0 : 100;
      this._mcPayIfSuccessHighlight._alpha = this._nPayBar != 2 ? 0 : 100;
      if(this.isClient)
      {
         if(this._nPayBar == 1)
         {
            this._cgPayIfSuccess.removeEventListener("dragItem",this);
            this._cgPay.addEventListener("dragItem",this);
         }
         else
         {
            this._cgPay.removeEventListener("dragItem",this);
            this._cgPayIfSuccess.addEventListener("dragItem",this);
         }
      }
   }
   function validateKama(nQuantity)
   {
      if(nQuantity > this.api.datacenter.Player.Kama)
      {
         nQuantity = this.api.datacenter.Player.Kama;
      }
      this.api.network.Exchange.movementPayKama(this._nPayBar,nQuantity);
   }
   function askKamaQuantity(nPayBar)
   {
      this.switchPayBar(nPayBar);
      var _loc3_ = this.gapi.loadUIComponent("PopupQuantity","PopupQuantity",{value:this.api.datacenter.Exchange.localKama,max:this.api.datacenter.Player.Kama,params:{targetType:"kama"}});
      _loc3_.addEventListener("validate",this);
   }
   function canUseItemInCraft(oItem)
   {
      if(this._nForgemagusItemType == undefined || this.isNotForgemagus())
      {
         return true;
      }
      if(oItem.type == 78)
      {
         return true;
      }
      var _loc3_ = false;
      var _loc4_ = 0;
      while(_loc4_ < dofus.graphics.gapi.ui.ForgemagusCraft.ITEMS_ALLOWED_AS_SIGNATURE.length)
      {
         if(dofus.graphics.gapi.ui.ForgemagusCraft.ITEMS_ALLOWED_AS_SIGNATURE[_loc4_] == oItem.unicID)
         {
            return true;
         }
         _loc4_ += 1;
      }
      var _loc5_ = 0;
      while(_loc5_ < dofus.graphics.gapi.ui.ForgemagusCraft.TYPES_ALLOWED_AS_COMPONENT.length)
      {
         if(dofus.graphics.gapi.ui.ForgemagusCraft.TYPES_ALLOWED_AS_COMPONENT[_loc5_] == oItem.type)
         {
            return true;
         }
         _loc5_ += 1;
      }
      if(this._nForgemagusItemType != oItem.type || !oItem.enhanceable)
      {
         return false;
      }
      return true;
   }
   function validCraft()
   {
      this.showCraftViewer(false);
      this._btnCraft.selected = false;
      this.recordGarbage();
      this.setReady();
   }
   function isNotForgemagus()
   {
      return _global.isNaN(this._nForgemagusItemType);
   }
   function addCraft(nTargetItemId)
   {
      if(this._nLastRegenerateTimer + dofus.graphics.gapi.ui.SecureCraft.NAME_GENERATION_DELAY >= getTimer())
      {
         return undefined;
      }
      this._nLastRegenerateTimer = getTimer();
      var _loc3_ = this.api.lang.getSkillText(this._nSkillId).cl;
      var _loc4_ = 0;
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
      while(_loc4_ < _loc3_.length)
      {
         _loc5_ = _loc3_[_loc4_];
         if(nTargetItemId == _loc5_)
         {
            _loc6_ = this.api.lang.getCraftText(_loc5_);
            _loc7_ = 0;
            _loc8_ = [];
            _loc9_ = 0;
            while(_loc9_ < _loc6_.length)
            {
               _loc10_ = _loc6_[_loc9_];
               _loc11_ = _loc10_[0];
               _loc12_ = _loc10_[1];
               _loc13_ = false;
               _loc14_ = 0;
               while(_loc14_ < this._eaDataProvider.length)
               {
                  if(_loc11_ == this._eaDataProvider[_loc14_].unicID)
                  {
                     if(_loc12_ <= this._eaDataProvider[_loc14_].Quantity)
                     {
                        _loc7_ += 1;
                        _loc13_ = true;
                        _loc8_.push({item:this._eaDataProvider[_loc14_],qty:_loc12_});
                        break;
                     }
                  }
                  _loc14_ += 1;
               }
               if(!_loc13_)
               {
                  break;
               }
               _loc9_ += 1;
            }
            if(_loc13_ && _loc6_.length == _loc7_)
            {
               _loc15_ = [];
               _loc16_ = 0;
               while(_loc16_ < this._cgLocal.dataProvider.length)
               {
                  _loc17_ = this._cgLocal.dataProvider[_loc16_];
                  _loc18_ = _loc17_.Quantity;
                  if(!(_loc18_ < 1 || _loc18_ == undefined))
                  {
                     _loc15_.push({Add:false,ID:_loc17_.ID,Quantity:_loc18_});
                  }
                  _loc16_ += 1;
               }
               _loc19_ = 0;
               while(_loc19_ < _loc8_.length)
               {
                  _loc17_ = _loc8_[_loc19_].item;
                  _loc18_ = _loc8_[_loc19_].qty;
                  if(!(_loc18_ < 1 || _loc18_ == undefined))
                  {
                     _loc15_.push({Add:true,ID:_loc17_.ID,Quantity:_loc18_});
                  }
                  _loc19_ += 1;
               }
               this.api.network.Exchange.movementItems(_loc15_);
               break;
            }
            this.api.kernel.showMessage(undefined,this.api.lang.getText("DONT_HAVE_ALL_INGREDIENT"),"ERROR_BOX");
            break;
         }
         _loc4_ += 1;
      }
   }
   function kamaChanged(oEvent)
   {
      this._lblKama.text = new ank.utils.ExtendedString(oEvent.value).addMiddleChar(this.api.lang.getConfigText("THOUSAND_SEPARATOR"),3);
   }
   function modelChanged(oEvent)
   {
      switch(oEvent.target)
      {
         case this._eaLocalDataProvider:
            this.updateLocalData();
            this.updatePreview();
            return undefined;
         case this._eaDistantDataProvider:
            this.updateDistantData();
            this.updatePreview();
            if(this._eaDistantDataProvider.length > 0)
            {
               this._cgCoop.dataProvider = new ank.utils.ExtendedArray();
               return undefined;
            }
            return undefined;
            break;
         case this._eaDataProvider:
            this.updateData();
            this.updatePreview();
            return undefined;
         case this._eaCoopDataProvider:
            this.updateCoopData();
            this.updatePreview();
            return undefined;
         case this._eaPayDataProvider:
            this.updatePayData(true);
            return undefined;
         case this._eaPayIfSuccessDataProvider:
            this.updatePayIfSuccessData(true);
            return undefined;
         case this._eaReadyDataProvider:
            this.updateReadyState();
            return undefined;
         default:
            this.updateData();
            this.updateLocalData();
            this.updateDistantData();
            this.updateCoopData();
            this.updatePayData();
            this.updatePayIfSuccessData();
            this.updatePreview();
            return undefined;
      }
   }
   function over(oEvent)
   {
      switch(oEvent.target._name)
      {
         case "_btnFilterEquipement":
            this.api.ui.showTooltip(this.api.lang.getText("EQUIPEMENT"),oEvent.target,-20);
            return;
         case "_btnFilterNonEquipement":
            this.api.ui.showTooltip(this.api.lang.getText("NONEQUIPEMENT"),oEvent.target,-20);
            return;
         case "_btnFilterRessoureces":
            this.api.ui.showTooltip(this.api.lang.getText("RESSOURECES"),oEvent.target,-20);
            return;
         case "_btnFilterSoul":
            this.api.ui.showTooltip(this.api.lang.getText("SOUL"),oEvent.target,-20);
            return;
         case "_btnFilterRunes":
            this.api.ui.showTooltip(this.api.lang.getText("RUNES"),oEvent.target,-20);
            return;
         case "_btnFilterCards":
            this.api.ui.showTooltip(this.api.lang.getText("CARDS"),oEvent.target,-20);
            return;
         case "_mcFiligrane":
            if(this._mcFiligrane.itemName != undefined)
            {
               this.gapi.showTooltip(this._mcFiligrane.itemName,this._ctrPreview,-22);
            }
      }
      return undefined;
   }
   function out(oEvent)
   {
      this.api.ui.hideTooltip();
   }
   function click(oEvent)
   {
      if(oEvent.target == this._btnClose)
      {
         this.callClose();
         return undefined;
      }
      var _loc3_;
      if(oEvent.target == this._btnValidate)
      {
         _loc3_ = this.api.kernel.GameManager.analyseReceipts(this.getTotalCraftInventory(),this._nSkillId,this._nMaxItem);
         if(_loc3_ == undefined && (this.api.kernel.OptionsManager.getOption("AskForWrongCraft") && this.isNotForgemagus()))
         {
            this.api.kernel.showMessage(this.api.lang.getText("INFORMATIONS"),this.api.lang.getText("WRONG_CRAFT_CONFIRM"),"CAUTION_YESNO",{name:"confirmWrongCraft",listener:this});
         }
         else
         {
            this.validCraft();
         }
         return undefined;
      }
      if(oEvent.target == this._btnCraft)
      {
         this.showCraftViewer(oEvent.target.selected);
         return undefined;
      }
      if(oEvent.target == this._btnPrivateMessage || oEvent.target == this._btnPrivateMessagePay)
      {
         this.api.kernel.GameManager.askPrivateMessage(this.api.datacenter.Sprites.getItemAt(this.api.datacenter.Exchange.distantPlayerID).name);
      }
      if(oEvent.target == this._btnPay || oEvent.target == this._btnValidatePay)
      {
         this._sCurrentDragSource = undefined;
         this._sCurrentDragTarget = undefined;
         this.switchToPayMode();
      }
      if(oEvent.target != this._btnSelectedFilterButton)
      {
         this._btnSelectedFilterButton.selected = false;
         this._btnSelectedFilterButton = oEvent.target;
         switch(oEvent.target._name)
         {
            case "_btnFilterEquipement":
               this._aSelectedSuperTypes = dofus.Constants.FILTER_EQUIPEMENT;
               this._lblFilter.text = this.api.lang.getText("EQUIPEMENT");
               break;
            case "_btnFilterNonEquipement":
               this._aSelectedSuperTypes = dofus.Constants.FILTER_NONEQUIPEMENT;
               this._lblFilter.text = this.api.lang.getText("NONEQUIPEMENT");
               break;
            case "_btnFilterRessoureces":
               this._aSelectedSuperTypes = dofus.Constants.FILTER_RESSOURCES;
               this._lblFilter.text = this.api.lang.getText("RESSOURECES");
               break;
            case "_btnFilterSoul":
               this._aSelectedSuperTypes = dofus.Constants.FILTER_SOUL;
               this._lblFilter.text = this.api.lang.getText("SOUL");
               break;
            case "_btnFilterRunes":
               this._aSelectedSuperTypes = dofus.Constants.FILTER_RUNES;
               this._lblFilter.text = this.api.lang.getText("RUNES");
               break;
            case "_btnFilterCards":
               this._aSelectedSuperTypes = dofus.Constants.FILTER_CARDS;
               this._lblFilter.text = this.api.lang.getText("CARDS");
         }
         this.updateData(true);
      }
      else
      {
         oEvent.target.selected = true;
      }
   }
   function overItem(oEvent)
   {
      var _loc3_ = oEvent.target;
      var _loc4_ = dofus.datacenter.Item(_loc3_.contentData);
      _loc4_.showStatsTooltip(_loc3_,_loc4_.style);
      this._currentOverContainer = _loc3_;
   }
   function outItem(oEvent)
   {
      this.gapi.hideTooltip();
      this._currentOverContainer = undefined;
   }
   function dblClickItem(oEvent)
   {
      var _loc3_ = oEvent.target.contentData;
      if(_loc3_ == undefined)
      {
         return undefined;
      }
      var _loc4_ = !Key.isDown(Key.CONTROL) ? 1 : _loc3_.Quantity;
      var _loc5_ = oEvent.owner._name;
      this._sCurrentDragSource = _loc5_;
      var _loc6_;
      switch(_loc5_)
      {
         case "_cgGrid":
            if(!this.canDropInGarbage(_loc3_))
            {
               return undefined;
            }
            if(!this.canUseItemInCraft(_loc3_))
            {
               this.api.kernel.showMessage(undefined,this.api.lang.getText("WRONG_ITEM_TYPE"),"ERROR_CHAT");
               return undefined;
            }
            if(!this._bPayMode)
            {
               _loc6_ = "_cgLocal";
               break;
            }
            if(this.isClient)
            {
               _loc6_ = this._nPayBar != 1 ? "_cgPayIfSuccess" : "_cgPay";
               break;
            }
            return undefined;
            break;
         case "_cgLocal":
            _loc6_ = "_cgGrid";
            break;
         case "_cgPay":
            this.switchPayBar(1);
            _loc6_ = "_cgGrid";
            break;
         case "_cgPayIfSuccess":
            this.switchPayBar(2);
            _loc6_ = "_cgGrid";
      }
      this.validateDrop(_loc6_,_loc3_,_loc4_);
   }
   function dragItem(oEvent)
   {
      this.gapi.removeCursor();
      if(oEvent.target.contentData == undefined)
      {
         return undefined;
      }
      this._sCurrentDragSource = oEvent.target._parent._parent._name;
      this.gapi.setCursor(oEvent.target.contentData);
   }
   function dropItem(oEvent)
   {
      var _loc3_ = this.gapi.getCursor();
      if(_loc3_ == undefined)
      {
         return undefined;
      }
      if(_loc3_.isShortcut)
      {
         return undefined;
      }
      this.gapi.removeCursor();
      var _loc4_ = oEvent.target._parent._parent._name;
      switch(_loc4_)
      {
         case "_cgGrid":
            if(!this._bPayMode)
            {
               if(_loc3_.position == -1)
               {
                  return undefined;
               }
            }
            break;
         case "_cgLocal":
            if(_loc3_.position == -2)
            {
               return undefined;
            }
            if(!this.canUseItemInCraft(_loc3_))
            {
               this.api.kernel.showMessage(undefined,this.api.lang.getText("WRONG_ITEM_TYPE"),"ERROR_CHAT");
               return undefined;
            }
            if(!this.canDropInGarbage(_loc3_))
            {
               return undefined;
            }
            break;
         case "_cgPay":
            if(this._sCurrentDragSource == "_cgPay" || this._sCurrentDragSource == "_cgPayIfSuccess")
            {
               return undefined;
            }
            this.switchPayBar(1);
            break;
         case "_cgPayIfSuccess":
            if(this._sCurrentDragSource == "_cgPay" || this._sCurrentDragSource == "_cgPayIfSuccess")
            {
               return undefined;
            }
            this.switchPayBar(2);
      }
      var _loc5_;
      if(_loc3_.Quantity > 1 && !(_loc4_ == "_cgGrid" && (this._sCurrentDragSource == "_cgPay" || this._sCurrentDragSource == "_cgPayIfSuccess")))
      {
         _loc5_ = this.gapi.loadUIComponent("PopupQuantity","PopupQuantity",{value:1,max:_loc3_.Quantity,params:{targetType:"item",oItem:_loc3_,targetGrid:_loc4_}});
         _loc5_.addEventListener("validate",this);
      }
      else
      {
         this.validateDrop(_loc4_,_loc3_,1);
      }
   }
   function selectItem(oEvent)
   {
      if(oEvent.target.contentData == undefined)
      {
         this.hideItemViewer(true);
      }
      else
      {
         if(Key.isDown(dofus.Constants.CHAT_INSERT_ITEM_KEY))
         {
            this.api.kernel.GameManager.insertItemInChat(oEvent.target.contentData);
            return undefined;
         }
         this.hideItemViewer(false);
         this._itvItemViewer.itemData = oEvent.target.contentData;
      }
   }
   function validate(oEvent)
   {
      switch(oEvent.params.targetType)
      {
         case "item":
            this.validateDrop(oEvent.params.targetGrid,oEvent.params.oItem,oEvent.value);
            return;
         case "kama":
            this.validateKama(oEvent.value);
      }
      return undefined;
   }
   function itemSelected(oEvent)
   {
      var _loc3_;
      if((_loc3_ = oEvent.target._name) === "_cbTypes")
      {
         this._nSelectedTypeID = this._cbTypes.selectedItem.id;
         this.api.datacenter.Basics[dofus.graphics.gapi.ui.SecureCraft.CLASS_NAME + "_subfilter_" + this._btnSelectedFilterButton._name] = this._nSelectedTypeID;
         this.updateData();
      }
   }
   function localKamaChange(oEvent)
   {
      this.hideButtonValidate(true);
      ank.utils.Timer.setTimer(this,"securecraft",this,this.hideButtonValidate,dofus.graphics.gapi.ui.SecureCraft.DELAY_BEFORE_VALIDATE,[false]);
   }
   function payKamaChange(oEvent)
   {
      this.switchToPayMode(true);
      if(dofus.Constants.TRIPLEFRAMERATE)
      {
         this._mcBlinkPay_TripleFramerate.play();
      }
      else
      {
         this._mcBlinkPay.play();
      }
      this._nKamaPayment = oEvent.value;
      if(_global.isNaN(this._nKamaPaymentIfSuccess) || this._nKamaPaymentIfSuccess == undefined)
      {
         this._nKamaPaymentIfSuccess = 0;
      }
      if(this.isClient)
      {
         this._lblKama.text = new ank.utils.ExtendedString(this.api.datacenter.Player.Kama - this._nKamaPayment - this._nKamaPaymentIfSuccess).addMiddleChar(this.api.lang.getConfigText("THOUSAND_SEPARATOR"),3);
      }
      this._lblPayKama.text = new ank.utils.ExtendedString(oEvent.value).addMiddleChar(this.api.lang.getConfigText("THOUSAND_SEPARATOR"),3);
      this.hideButtonValidate(true);
      ank.utils.Timer.setTimer(this,"securecraft",this,this.hideButtonValidate,dofus.graphics.gapi.ui.SecureCraft.DELAY_BEFORE_VALIDATE,[false]);
   }
   function payIfSuccessKamaChange(oEvent)
   {
      this.switchToPayMode(true);
      if(dofus.Constants.TRIPLEFRAMERATE)
      {
         this._mcBlinkPayIfSuccess_TripleFramerate.play();
      }
      else
      {
         this._mcBlinkPayIfSuccess.play();
      }
      this._nKamaPaymentIfSuccess = oEvent.value;
      if(_global.isNaN(this._nKamaPayment) || this._nKamaPayment == undefined)
      {
         this._nKamaPayment = 0;
      }
      if(this.isClient)
      {
         this._lblKama.text = new ank.utils.ExtendedString(this.api.datacenter.Player.Kama - this._nKamaPayment - this._nKamaPaymentIfSuccess).addMiddleChar(this.api.lang.getConfigText("THOUSAND_SEPARATOR"),3);
      }
      this._lblPayIfSuccessKama.text = new ank.utils.ExtendedString(oEvent.value).addMiddleChar(this.api.lang.getConfigText("THOUSAND_SEPARATOR"),3);
      this.hideButtonValidate(true);
      ank.utils.Timer.setTimer(this,"securecraft",this,this.hideButtonValidate,dofus.graphics.gapi.ui.SecureCraft.DELAY_BEFORE_VALIDATE,[false]);
   }
   function yes()
   {
      this.validCraft();
   }
}
