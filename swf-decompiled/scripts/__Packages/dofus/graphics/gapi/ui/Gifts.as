class dofus.graphics.gapi.ui.Gifts extends dofus.graphics.gapi.core.DofusAdvancedComponent
{
   var _aSpriteList;
   var _bAttributingGifts;
   var _btnClose;
   var _btnSelect;
   var _btnViewAllGifts;
   var _cgGifts;
   var _itvItemViewer;
   var _lblDateGift;
   var _lblGift;
   var _lblItems;
   var _lblSelectCharacter;
   var _lblTitle;
   var _lblTitleGift;
   var _ldrGfx;
   var _mcColSeparator;
   var _mcGiftsWarning;
   var _mcItvDescBg;
   var _mcItvEffectsBg;
   var _mcItvIconBg;
   var _mcTopBrownRectangle;
   var _nLastAttributionCharacterID;
   var _nSaveLastClick;
   var _nSelectedIndex;
   var _pgBtnCheckAll;
   var _pgBtnChooseCharacter;
   var _pgBtnClose;
   var _pgHeaderRectangle;
   var _pgLblCheckedCount;
   var _pgLstGifts;
   var _pgMovieClips;
   var _pgTxtInfos;
   var _pgWinBackground;
   var _taMultipleGiftsList;
   var _txtDescription;
   var _visible;
   var addToQueue;
   var gapi;
   var unloadThis;
   static var CLASS_NAME = "Gifts";
   var _aPendingAttributionGifts = [];
   var _nCurrentGiftsToAttributeCount = 0;
   var _nTotalGiftsToAttributeCount = 0;
   function Gifts()
   {
      super();
   }
   function get firstGiftInStack()
   {
      var _loc2_ = this.api.datacenter.Basics.aks_gifts_stack;
      return _loc2_[0];
   }
   function set spriteList(aSpriteList)
   {
      this._aSpriteList = aSpriteList;
   }
   function get isAttributingGifts()
   {
      return this._bAttributingGifts;
   }
   function checkNextGift()
   {
      var _loc2_;
      if(!this._bAttributingGifts && (this._nCurrentGiftsToAttributeCount > 1 && this._nCurrentGiftsToAttributeCount == this._nTotalGiftsToAttributeCount))
      {
         _loc2_ = this["_ccs" + this._nSelectedIndex].data.name;
         this.api.kernel.showMessage(this.api.lang.getText("INFORMATIONS"),this.api.lang.getText("GIFTS_MASS_DISTRIBUTION_FINISHED_SUCCESS",[_loc2_,this._nTotalGiftsToAttributeCount]),"ERROR_BOX");
      }
      var _loc3_;
      if(this._bAttributingGifts && this._nCurrentGiftsToAttributeCount < this._nTotalGiftsToAttributeCount)
      {
         _loc3_ = _global.setTimeout(this,"attributeGiftToCharacter",200,this._nLastAttributionCharacterID);
      }
      else if(this.api.datacenter.Basics.aks_gifts_stack.length != 0)
      {
         this.api.ui.unloadUIComponent("WaitingMessage");
         this.gapi.loadUIComponent("Gifts","Gifts",{spriteList:this._aSpriteList},{bForceLoad:true});
      }
      else
      {
         this.api.ui.unloadUIComponent("WaitingMessage");
         this.gapi.getUIComponent("ChooseCharacter")._visible = true;
         this.gapi.getUIComponent("CreateCharacter")._visible = true;
         this.gapi.getUIComponent("ChooseCharacter")._btnViewAllGifts._visible = false;
         this.unloadThis();
      }
      this.gapi.getUIComponent("ChooseCharacter")._mcGiftsWarning._visible = this.firstGiftInStack.date != "";
   }
   function init()
   {
      super.init(false,dofus.graphics.gapi.ui.Gifts.CLASS_NAME);
   }
   function callClose()
   {
      if(this._pgWinBackground._visible)
      {
         this.showViewPendingGifts(false);
         return true;
      }
      this.gapi.getUIComponent("ChooseCharacter")._visible = true;
      this.gapi.getUIComponent("CreateCharacter")._visible = true;
      this.unloadThis();
      return true;
   }
   function createChildren()
   {
      this._pgMovieClips = [this._pgWinBackground,this._pgHeaderRectangle,this._pgLstGifts,this._pgBtnClose,this._pgTxtInfos,this._pgBtnCheckAll,this._pgLblCheckedCount,this._pgBtnChooseCharacter];
      this._visible = false;
      this.showViewPendingGifts(false);
      this.addToQueue({object:this,method:this.addListeners});
      this.addToQueue({object:this,method:this.initTexts});
      this.addToQueue({object:this,method:this.initData});
   }
   function addListeners()
   {
      var _loc2_ = 0;
      var _loc3_;
      while(_loc2_ < 5)
      {
         _loc3_ = this["_ccs" + _loc2_];
         _loc3_.params = {index:_loc2_};
         _loc3_.addEventListener("select",this);
         _loc2_ = _loc2_ + 1;
      }
      this._cgGifts.addEventListener("selectItem",this);
      this._cgGifts.multipleContainerSelectionEnabled = false;
      this._btnClose.addEventListener("click",this);
      this._btnSelect.addEventListener("click",this);
      this._btnViewAllGifts.addEventListener("click",this);
      this._pgBtnClose.addEventListener("click",this);
      this._pgBtnChooseCharacter.addEventListener("click",this);
      this._pgBtnCheckAll.addEventListener("click",this);
      this._pgBtnCheckAll.addEventListener("over",this);
      this._pgBtnCheckAll.addEventListener("out",this);
      this._cgGifts.addEventListener("overItem",this);
      this._cgGifts.addEventListener("outItem",this);
      this._lblDateGift.addEventListener("over",this);
      this._lblDateGift.addEventListener("out",this);
      var ref = this;
      this._mcGiftsWarning.onRollOver = function()
      {
         ref.over({target:this});
      };
      this._mcGiftsWarning.onRollOut = function()
      {
         ref.out({target:this});
      };
   }
   function initTexts()
   {
      this._pgBtnCheckAll.label = this.api.lang.getText("SELECT_UNSELECT_ALL_BUTTON");
      this._lblGift.text = this.api.lang.getText("THE_GIFT");
      this._lblItems.text = this.api.lang.getText("GIFT_CONTENT");
      this._btnClose.label = this.api.lang.getText("CLOSE");
      this._btnViewAllGifts.label = this.api.lang.getText("MY_GIFTS");
      this._btnSelect.label = this.api.lang.getText("SELECT");
      this._pgWinBackground.title = this.api.lang.getText("MY_GIFTS");
      this._pgTxtInfos.text = this.api.lang.getText("GIFTS_VIEWER_INFOS");
   }
   function showViewPendingGifts(bShow)
   {
      var _loc3_ = 0;
      var _loc4_;
      while(_loc3_ < this._pgMovieClips.length)
      {
         _loc4_ = this._pgMovieClips[_loc3_];
         _loc4_._visible = bShow;
         _loc3_ = _loc3_ + 1;
      }
   }
   function initData()
   {
      var _loc2_ = this.firstGiftInStack;
      var _loc0_;
      var _loc3_;
      var _loc4_;
      if((_loc0_ = _loc2_.type) !== 1)
      {
         this.checkNextGift();
      }
      else
      {
         this._visible = true;
         this._cgGifts.dataProvider = _loc2_.items;
         this._cgGifts.selectedIndex = 0;
         this._itvItemViewer.itemData = _loc2_.items[0];
         this._ldrGfx.contentPath = _loc2_.gfxUrl;
         this._lblTitleGift.text = _loc2_.title;
         this._txtDescription.text = _loc2_.desc;
         this._lblDateGift.text = _loc2_.date != "" ? this.api.lang.getText("EXPIRATION_DATE",[_loc2_.date]) : "";
         this._lblDateGift.enableOverEvents = true;
         _loc3_ = 0;
         while(_loc3_ < 5)
         {
            _loc4_ = this["_ccs" + _loc3_];
            _loc4_.data = this._aSpriteList[_loc3_];
            _loc4_.enabled = this._aSpriteList[_loc3_] != undefined;
            _loc3_ = _loc3_ + 1;
         }
         this.refreshGiftsList();
      }
   }
   function refreshGiftsList()
   {
      var _loc2_ = new ank.utils.ExtendedArray();
      var _loc3_ = this.firstGiftInStack;
      _loc3_.forbidPlayerChoice = true;
      _loc3_.playerWantsAttribution = true;
      _loc3_.indexOnGiftsStack = -1;
      _loc3_.addEventListener("giftAttributionStateChanged",this);
      _loc2_.push(_loc3_);
      var _loc4_ = this.api.datacenter.Basics.aks_gifts_stack;
      var _loc5_ = false;
      var _loc6_ = false;
      var _loc7_ = false;
      var _loc8_ = 1;
      this._aPendingAttributionGifts = [];
      this._aPendingAttributionGifts.push(_loc3_);
      var _loc9_ = 1;
      var _loc10_;
      while(_loc9_ < _loc4_.length)
      {
         _loc10_ = _loc4_[_loc9_];
         _loc10_.addEventListener("giftAttributionStateChanged",this);
         if(!_loc10_.playerWantsAttribution)
         {
            _loc5_ = true;
         }
         else if(_loc5_)
         {
            _loc10_.playerWantsAttribution = false;
         }
         if(_loc6_)
         {
            _loc10_.forbidPlayerChoice = true;
         }
         else
         {
            _loc10_.forbidPlayerChoice = false;
         }
         if(_loc10_.playerWantsAttribution)
         {
            _loc8_ = _loc8_ + 1;
            this._aPendingAttributionGifts.push(_loc10_);
         }
         else
         {
            _loc6_ = true;
         }
         if(_loc10_.date != "")
         {
            _loc7_ = true;
         }
         _loc10_.indexOnGiftsStack = _loc9_;
         _loc2_.push(_loc10_);
         _loc9_ = _loc9_ + 1;
      }
      this._pgLstGifts.dataProvider = _loc2_;
      var _loc11_ = _loc8_ > 1;
      this._ldrGfx._visible = !_loc11_;
      this._lblGift._visible = !_loc11_;
      this._lblItems._visible = !_loc11_;
      this._lblTitleGift._visible = !_loc11_;
      this._txtDescription._visible = !_loc11_;
      this._lblDateGift._visible = !_loc11_;
      this._cgGifts._visible = !_loc11_;
      this._itvItemViewer._visible = !_loc11_;
      this._mcItvIconBg._visible = !_loc11_;
      this._mcItvEffectsBg._visible = !_loc11_;
      this._mcItvDescBg._visible = !_loc11_;
      this._mcColSeparator._visible = !_loc11_;
      this._mcTopBrownRectangle._visible = !_loc11_;
      var _loc12_;
      var _loc13_;
      var _loc14_;
      if(!_loc11_)
      {
         this._taMultipleGiftsList.text = "";
         this._lblTitle.text = this.api.lang.getText("GIFTS_TITLE");
         this._pgLblCheckedCount.text = this.api.lang.getText("MASS_GIFTS_NOT_CONFIGURED_YET");
         this._pgBtnChooseCharacter.label = this.api.lang.getText("GIFTS_SELECT_CHARACTER_ONE_GIFT");
         this._lblSelectCharacter.text = this.api.lang.getText("GIFT_SELECT_CHARACTER");
      }
      else
      {
         _loc12_ = [];
         _loc13_ = 0;
         while(_loc13_ < this._aPendingAttributionGifts.length)
         {
            _loc14_ = this._aPendingAttributionGifts[_loc13_];
            _loc12_.push(_loc14_.title);
            _loc13_ = _loc13_ + 1;
         }
         this._taMultipleGiftsList.text = this.api.lang.getText("GIFTS_MASS_DISTRIBUTION_HELP",[_loc8_,_loc12_.join("\n- ")]);
         this._lblTitle.text = this.api.lang.getText("GIFTS_UI_TITLE_ON_MASS",[_loc8_]);
         this._pgLblCheckedCount.text = this.api.lang.getText("GIFTS_MASS_DISTRIBUTION",[_loc8_]);
         this._pgBtnChooseCharacter.label = this.api.lang.getText("GIFTS_SELECT_CHARACTER_MASS_GIFTS",[_loc8_]);
         this._lblSelectCharacter.text = this.api.lang.getText("GIFTS_SELECT_RECEIVER");
      }
      this._mcGiftsWarning._visible = _loc7_;
   }
   function autoSelectUnselectAllGifts()
   {
      var _loc2_ = this.api.datacenter.Basics.aks_gifts_stack;
      var _loc3_;
      var _loc4_;
      var _loc5_;
      var _loc6_;
      if(this._aPendingAttributionGifts.length == _loc2_.length)
      {
         _loc3_ = 1;
         while(_loc3_ < _loc2_.length)
         {
            _loc4_ = _loc2_[_loc3_];
            _loc4_.playerWantsAttribution = false;
            _loc3_ = _loc3_ + 1;
         }
      }
      else
      {
         _loc5_ = 1;
         while(_loc5_ < _loc2_.length)
         {
            _loc6_ = _loc2_[_loc5_];
            _loc6_.playerWantsAttribution = true;
            _loc5_ = _loc5_ + 1;
         }
      }
      this.refreshGiftsList();
   }
   function giftAttributionStateChanged(oEvent)
   {
      this.refreshGiftsList();
   }
   function select(oEvent)
   {
      var _loc3_ = oEvent.target.params.index;
      this["_ccs" + this._nSelectedIndex].selected = false;
      if(this._nSelectedIndex == _loc3_)
      {
         delete this._nSelectedIndex;
      }
      else
      {
         this._nSelectedIndex = _loc3_;
      }
      if(getTimer() - this._nSaveLastClick < ank.gapi.Gapi.DBLCLICK_DELAY)
      {
         this._nSelectedIndex = _loc3_;
         this.click({target:this._btnSelect});
         return undefined;
      }
      this._nSaveLastClick = getTimer();
   }
   function selectItem(oEvent)
   {
      this._itvItemViewer.itemData = oEvent.target.contentData;
   }
   function click(oEvent)
   {
      var _loc3_;
      var _loc4_;
      var _loc5_;
      var _loc6_;
      var _loc7_;
      switch(oEvent.target)
      {
         case this._pgBtnCheckAll:
            this.autoSelectUnselectAllGifts();
            break;
         case this._pgBtnClose:
         case this._pgBtnChooseCharacter:
            this.showViewPendingGifts(false);
            break;
         case this._btnViewAllGifts:
            this.showViewPendingGifts(!this._pgWinBackground._visible);
            break;
         case this._btnClose:
            this.callClose();
            break;
         case this._btnSelect:
            if(!_global.isNaN(this._nSelectedIndex))
            {
               _loc3_ = this._aPendingAttributionGifts.length;
               _loc4_ = this["_ccs" + this._nSelectedIndex].data.name;
               _loc5_ = _loc3_ > 1;
               if(_loc5_)
               {
                  this.api.kernel.showMessage(this.api.lang.getText("MY_GIFTS"),this.api.lang.getText("GIFT_MASS_ATTRIBUTION_CONFIRMATION",[_loc3_,_loc4_]),"CAUTION_YESNO",{name:"GiftAttribution",listener:this,params:{charId:this["_ccs" + this._nSelectedIndex].data.id}});
               }
               else
               {
                  _loc6_ = this.firstGiftInStack;
                  _loc7_ = dofus.datacenter.Item(_loc6_.items[0]);
                  this.api.kernel.showMessage(this.api.lang.getText("THE_GIFT"),this.api.lang.getText("GIFT_ATTRIBUTION_CONFIRMATION",[_loc7_.name,_loc4_]),"CAUTION_YESNO",{name:"GiftAttribution",listener:this,params:{charId:this["_ccs" + this._nSelectedIndex].data.id}});
               }
            }
            else
            {
               this.api.kernel.showMessage(undefined,this.api.lang.getText("SELECT_CHARACTER"),"ERROR_BOX",{name:"NoSelect"});
            }
         default:
            return;
      }
   }
   function over(oEvent)
   {
      switch(oEvent.target)
      {
         case this._pgBtnCheckAll:
            this.api.ui.showTooltip(this.api.lang.getText("SELECT_UNSELECT_ALL_BUTTON_HELP"),oEvent.target,-40,{bXLimit:true,bYLimit:false});
            break;
         case this._lblDateGift:
            this.api.ui.showTooltip(this.api.lang.getText("EXPIRATION_GIFT",[this.firstGiftInStack.date]),oEvent.target,0,{bTopAlign:true});
            break;
         case this._mcGiftsWarning:
            this.api.ui.showTooltip(this.api.lang.getText("EXPIRATION_GIFTS",[this.firstGiftInStack.date]),oEvent.target,-20);
         default:
            return;
      }
   }
   function out(oEvent)
   {
      switch(oEvent.target)
      {
         case this._pgBtnCheckAll:
         case this._lblDateGift:
         case this._mcGiftsWarning:
            this.api.ui.hideTooltip();
         default:
            return;
      }
   }
   function overItem(oEvent)
   {
      var _loc3_ = oEvent.target;
      var _loc4_ = dofus.datacenter.Item(_loc3_.contentData);
      _loc4_.showStatsTooltip(_loc3_,_loc4_.style);
   }
   function outItem(oEvent)
   {
      this.gapi.hideTooltip();
   }
   function yes(oEvent)
   {
      var _loc0_;
      if((_loc0_ = oEvent.target._name) === "AskYesNoGiftAttribution")
      {
         this.attributeGiftToCharacter(oEvent.params.charId);
      }
   }
   function attributeGiftToCharacter(nCharID)
   {
      var _loc3_ = dofus.datacenter.Gift(this.api.datacenter.Basics.aks_gifts_stack.shift());
      if(!this._bAttributingGifts)
      {
         this._bAttributingGifts = true;
         this._nCurrentGiftsToAttributeCount = 0;
         this._nTotalGiftsToAttributeCount = this._aPendingAttributionGifts.length;
      }
      this._nLastAttributionCharacterID = nCharID;
      this._nCurrentGiftsToAttributeCount = this._nCurrentGiftsToAttributeCount + 1;
      if(this._nCurrentGiftsToAttributeCount == this._nTotalGiftsToAttributeCount)
      {
         this._bAttributingGifts = false;
      }
      this.api.network.Account.attributeGiftToCharacter(_loc3_.id,nCharID);
      this._visible = false;
      this.api.ui.loadUIComponent("WaitingMessage","WaitingMessage",{text:this.api.lang.getText("WAITING_MSG_RECORDING") + " (" + this.api.lang.getText("GIFT") + " " + this._nCurrentGiftsToAttributeCount + "/" + this._nTotalGiftsToAttributeCount + ")"},{bAlwaysOnTop:true,bForceLoad:true});
   }
}
