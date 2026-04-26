class dofus.datacenter.Item extends Object
{
   var _aEffectZones;
   var _aEffects;
   var _bCanBeExchange;
   var _bIsIncarnation;
   var _bIsSkineable;
   var _bUndestroyable;
   var _bUnknownSkinsCount;
   var _durability;
   var _durabilityMax;
   var _isLock;
   var _isTemporaryLock;
   var _itemDateId;
   var _itemLevel;
   var _itemName;
   var _itemPrice;
   var _itemType;
   var _itemWeight;
   var _nAveragePrice;
   var _nCustomMoneyItemId;
   var _nID;
   var _nLivingXp;
   var _nMood;
   var _nNbSkin;
   var _nPosition;
   var _nPrice;
   var _nPriceMultiplicator;
   var _nQuantity;
   var _nRealType;
   var _nRealUnicId;
   var _nRemainingHours;
   var _nResellCustomPrice;
   var _nSkin;
   var _nUnicID;
   var _oSkinItemInfos;
   var _oUnicInfos;
   var _rideItemDurability;
   var _rideItemDurabilityMax;
   var _sEffects;
   var _sGfx;
   var _sRealGfx;
   var _skinDurability;
   var _skinDurabilityMax;
   static var api;
   static var CLOSE_COMBAT_AS_ITEM_SPELL_ID = -2;
   static var OBJECT_ACTION_SUMMON = 623;
   static var OBJECT_ACTION_SUMMON_RANDOM_GRADE = 628;
   static var OBJECT_ACTION_REPLACE = 2150;
   static var OBJECT_ACTION_LINK_CHARACTER = 2151;
   static var OBJECT_ACTION_CUSTOM_SKIN = 969;
   static var OBJECT_ACTION_GIVE_AURA = 723;
   static var OBJECT_ACTION_GIVE_TITLE = 724;
   static var OBJECT_ACTION_INCARNATION = 669;
   static var OBJECT_ACTION_LEARN_EMOTICON = 10;
   static var OBJECT_ACTION_LINK_ACCOUNT = 983;
   static var OBJECT_ACTION_LAST_MEAL = 808;
   static var OBJECT_ACTION_PET_LIFE = 800;
   static var OBJECT_ACTION_CRAFTED_BY_CHARACTER = 988;
   static var OBJECT_ACTION_FM_BY_CHARACTER = 985;
   static var OBJECT_ACTION_SPELL_BOOSTS = [292,291,290,289,288,287,286,285,284,283,282,281];
   static var OBJECT_ACTION_LOCK = 2155;
   static var OBJECT_ACTION_LOCK_TEMPORARY = 2154;
   static var ENHANCEABLE_SUPER_TYPE = [1,2,3,4,5,10,11];
   static var NO_ENHANCEABLE_TYPE = [20,21,22,102,114];
   static var LEVEL_STEP = [0,10,21,33,46,60,75,91,108,126,145,165,186,208,231,255,280,306,333,361];
   static var DATE_ID = 0;
   static var TYPE_TTG_BOOSTER = 123;
   static var SUPERTYPE_TTG = 24;
   var _bRemovedFromInventory = false;
   var _bPlayerLocked = false;
   static var TYPE_FULL_SOUL_STONE_NORMAL = 85;
   static var TYPE_FULL_SOUL_STONE_BOSS = 124;
   static var TYPE_FULL_SOUL_STONE_ARCHI = 125;
   static var TYPE_PET = 18;
   function Item(nID, nUnicID, nQuantity, nPosition, sEffects, nPrice, nSkin, nMood)
   {
      super();
      this.initialize(nID,nUnicID,nQuantity,nPosition,sEffects,nPrice,nSkin,nMood);
   }
   static function isFullSoul(nType)
   {
      return nType == dofus.datacenter.Item.TYPE_FULL_SOUL_STONE_NORMAL || (nType == dofus.datacenter.Item.TYPE_FULL_SOUL_STONE_ARCHI || nType == dofus.datacenter.Item.TYPE_FULL_SOUL_STONE_BOSS);
   }
   function get isShortcut()
   {
      return false;
   }
   function get label()
   {
      if(this._nQuantity < 2)
      {
         return undefined;
      }
      var _loc2_ = String(this._nQuantity);
      if(_loc2_.length > 5)
      {
         if(_loc2_.length > 6)
         {
            _loc2_ = _loc2_.substring(0,4) + "k";
         }
         else
         {
            _loc2_ = _loc2_.substring(0,3) + "k";
         }
      }
      return _loc2_;
   }
   function get ID()
   {
      return this._nID;
   }
   function get unicID()
   {
      return this._nUnicID;
   }
   function get compressedEffects()
   {
      return this._sEffects;
   }
   function set Quantity(value)
   {
      if(_global.isNaN(Number(value)))
      {
         return;
      }
      this._nQuantity = Number(value);
   }
   function get Quantity()
   {
      return this._nQuantity;
   }
   function get isRemovedFromInventory()
   {
      return this._bRemovedFromInventory;
   }
   function set isRemovedFromInventory(bRemovedFromInventory)
   {
      this._bRemovedFromInventory = bRemovedFromInventory;
   }
   function get isPlayerLocked()
   {
      return this._bPlayerLocked;
   }
   function set isPlayerLocked(bLocked)
   {
      this._bPlayerLocked = bLocked;
   }
   function set remainingHours(nRemainingHours)
   {
      this._nRemainingHours = nRemainingHours;
   }
   function get remainingHours()
   {
      return this._nRemainingHours;
   }
   function set position(value)
   {
      if(_global.isNaN(Number(value)))
      {
         return;
      }
      this._nPosition = Number(value);
   }
   function get position()
   {
      return this._nPosition;
   }
   function get isEquiped()
   {
      return this._nPosition > -1;
   }
   function set priceMultiplicator(value)
   {
      if(_global.isNaN(Number(value)))
      {
         return;
      }
      this._nPriceMultiplicator = Number(value);
   }
   function get priceMultiplicator()
   {
      return this._nPriceMultiplicator;
   }
   function get averagePrice()
   {
      return this._nAveragePrice;
   }
   function set averagePrice(nAveragePrice)
   {
      this._nAveragePrice = nAveragePrice;
   }
   function get name()
   {
      var _loc2_ = ank.utils.PatternDecoder.getDescription(dofus.datacenter.Item.api.lang.fetchString(this._oUnicInfos.n),dofus.datacenter.Item.api.lang.getItemUnicStringText());
      var _loc3_;
      if(dofus.Constants.DEBUG)
      {
         _loc3_ = " (" + this.unicID;
         if(this.ID > 0)
         {
            _loc3_ += ", " + this.ID;
         }
         _loc3_ += ")";
         _loc2_ += _loc3_;
      }
      return _loc2_;
   }
   function get nameUppercase()
   {
      var _loc2_ = ank.utils.PatternDecoder.getDescription(dofus.datacenter.Item.api.lang.fetchString(this._oUnicInfos.nn),dofus.datacenter.Item.api.lang.getItemUnicStringText());
      var _loc3_;
      if(dofus.Constants.DEBUG)
      {
         _loc3_ = " (" + this.unicID;
         if(this.ID > 0)
         {
            _loc3_ += ", " + this.ID;
         }
         _loc3_ += ")";
         _loc2_ += _loc3_;
      }
      return _loc2_;
   }
   function get description()
   {
      var _loc2_ = dofus.datacenter.Item.api.lang.getItemTypeText(this.type).n;
      if(this.isCeremonial)
      {
         _loc2_ += " (" + dofus.datacenter.Item.api.lang.getText("ITEM_TYPE_CEREMONIAL") + ")";
      }
      var _loc3_ = "";
      var _loc4_;
      if(this.isFromItemSet)
      {
         _loc4_ = new dofus.datacenter.ItemSet(this.itemSetID);
         _loc3_ = "<u>" + _loc4_.name + " (" + dofus.datacenter.Item.api.lang.getText("ITEM_TYPE") + " : " + _loc2_ + ")</u>\n";
      }
      else
      {
         _loc3_ = "<u>" + dofus.datacenter.Item.api.lang.getText("ITEM_TYPE") + " : " + _loc2_ + "</u>\n";
      }
      _loc3_ += ank.utils.PatternDecoder.getDescription(dofus.datacenter.Item.api.lang.fetchString(this._oUnicInfos.d),dofus.datacenter.Item.api.lang.getItemUnicStringText());
      if(this.isCeremonial)
      {
         _loc3_ += " " + dofus.datacenter.Item.api.lang.getText("SUPERTYPE_" + this.superType + "_CERMONIAL_DESCRIPTION");
      }
      return _loc3_;
   }
   function get type()
   {
      if(this._nRealType)
      {
         return this._nRealType;
      }
      return Number(this._oUnicInfos.t);
   }
   function get isCardType()
   {
      var _loc2_ = this.type;
      return _loc2_ >= 119 && _loc2_ <= 122;
   }
   function get displayCeremonial()
   {
      var _loc2_ = this.type;
      return _loc2_ != 90 && _loc2_ != 77;
   }
   function get isCeremonial()
   {
      return !!this._oUnicInfos.ce;
   }
   function set type(nType)
   {
      this._nRealType = nType;
   }
   function get realType()
   {
      return Number(this._oUnicInfos.t);
   }
   function get enhanceable()
   {
      return !!this._oUnicInfos.fm;
   }
   function get isEnhanceableSuperType()
   {
      for(var _loc2_ in dofus.datacenter.Item.ENHANCEABLE_SUPER_TYPE)
      {
         if(dofus.datacenter.Item.ENHANCEABLE_SUPER_TYPE[_loc2_] == this.superType)
         {
            return true;
         }
      }
      return false;
   }
   function get canBeEnhanceableType()
   {
      for(var _loc2_ in dofus.datacenter.Item.NO_ENHANCEABLE_TYPE)
      {
         if(dofus.datacenter.Item.NO_ENHANCEABLE_TYPE[_loc2_] == this.type)
         {
            return false;
         }
      }
      return true;
   }
   function get hasFragment()
   {
      var _loc2_;
      if(this._aEffects != undefined)
      {
         _loc2_ = 0;
         while(_loc2_ < this._aEffects.length)
         {
            if(this._aEffects[_loc2_][0] == 2160)
            {
               return true;
            }
            _loc2_ += 1;
         }
      }
      return false;
   }
   function get isReallyEnhanceable()
   {
      return !this.isCeremonial && (this.enhanceable && (this.isEnhanceableSuperType && (this.canBeEnhanceableType && !this.hasFragment)));
   }
   function get canDisplayExoOver()
   {
      return !this.isCeremonial && (this.enhanceable && (this.isEnhanceableSuperType && this.canBeEnhanceableType));
   }
   function get style()
   {
      if(this.isCeremonial)
      {
         return "Ceremonial";
      }
      if(this.isFromItemSet)
      {
         return "ItemSet";
      }
      if(this.isEthereal)
      {
         return "Ethereal";
      }
      return "";
   }
   function get needTwoHands()
   {
      return this._oUnicInfos.tw == true;
   }
   function get isEthereal()
   {
      return this._oUnicInfos.et == true;
   }
   function get isHidden()
   {
      return this._oUnicInfos.h == true;
   }
   function get isUndestroyable()
   {
      return this._bUndestroyable || (this.realType == 113 || (this.skinItemInfos != undefined || this.isLock));
   }
   function get hasUnknownSkinsCount()
   {
      return this._bUnknownSkinsCount;
   }
   function get isFromItemSet()
   {
      return this._oUnicInfos.s != undefined;
   }
   function get itemSetID()
   {
      return this._oUnicInfos.s;
   }
   function get typeText()
   {
      return dofus.datacenter.Item.api.lang.getItemTypeText(this.type);
   }
   function get superType()
   {
      return this.typeText.t;
   }
   function get superTypeText()
   {
      return dofus.datacenter.Item.api.lang.getItemSuperTypeText(this.superType);
   }
   function get iconFile()
   {
      return dofus.Constants.ITEMS_PATH + this.type + "/" + this.gfx + ".swf";
   }
   function get effects()
   {
      return dofus.datacenter.Item.getItemDescriptionEffects(this._aEffects,dofus.datacenter.Item.getBaseItemEffects(this.unicID),undefined,this.canDisplayExoOver);
   }
   function get visibleEffects()
   {
      return dofus.datacenter.Item.getItemDescriptionEffects(this._aEffects,dofus.datacenter.Item.getBaseItemEffects(this.unicID),true,this.canDisplayExoOver);
   }
   function get canUse()
   {
      return this._oUnicInfos.u != undefined ? true : false;
   }
   function get canTarget()
   {
      return this._oUnicInfos.ut != undefined ? true : false;
   }
   function get canDestroy()
   {
      return this.superType != 14 && (!this.isCursed && !this._bUndestroyable);
   }
   function get canBeReinitializedByPlayer()
   {
      return this._oUnicInfos.prp;
   }
   function get canDrop()
   {
      return this.superType != 14 && (!this.isCursed && !this._bUndestroyable);
   }
   function get isCaptureItem()
   {
      return this.superType == 8;
   }
   function get level()
   {
      return Number(this._oUnicInfos.l);
   }
   function get gfx()
   {
      if(this._sGfx)
      {
         return this._sGfx;
      }
      if(this._oSkinItemInfos != undefined && this.displayCeremonial)
      {
         return this._oSkinItemInfos.g;
      }
      return this._oUnicInfos.g;
   }
   function set gfx(sGfx)
   {
      this._sGfx = sGfx;
   }
   function get realGfx()
   {
      if(this._oSkinItemInfos != undefined && this.displayCeremonial)
      {
         return this._oSkinItemInfos.g;
      }
      return this._sRealGfx;
   }
   function get price()
   {
      if(this._nPrice != undefined)
      {
         return this._nPrice;
      }
      if(this._nResellCustomPrice != undefined)
      {
         return this._nResellCustomPrice;
      }
      if(this._nPrice == undefined)
      {
         return Math.max(0,Math.round(Number(this._oUnicInfos.p) * (this._nPriceMultiplicator != undefined ? this._nPriceMultiplicator : 0)));
      }
   }
   function set price(nPrice)
   {
      this._nPrice = nPrice;
   }
   function get hasCustomResellCustomPrice()
   {
      return !_global.isNaN(this._nResellCustomPrice);
   }
   function get resellCustomPrice()
   {
      return this._nResellCustomPrice;
   }
   function set resellCustomPrice(nResellCustomPrice)
   {
      this._nResellCustomPrice = nResellCustomPrice;
   }
   function get customMoneyItemId()
   {
      return this._nCustomMoneyItemId;
   }
   function set customMoneyItemId(nCustomMoneyItemId)
   {
      this._nCustomMoneyItemId = nCustomMoneyItemId;
   }
   function get hasCustomMoneyItemId()
   {
      return !_global.isNaN(this._nCustomMoneyItemId) && this._nCustomMoneyItemId > 0;
   }
   function get weight()
   {
      return Number(this._oUnicInfos.w);
   }
   function get isCursed()
   {
      return this._oUnicInfos.m;
   }
   function get normalHit()
   {
      return this._aEffects;
   }
   function get criticalHitBonus()
   {
      return this.getItemFightEffectsText(0);
   }
   function get apCost()
   {
      var _loc2_ = dofus.datacenter.Item.api.kernel.SpellsBoostsManager.getSpellModificator(dofus.managers.SpellsBoostsManager.ACTION_BOOST_ITEM_AP_COST,dofus.datacenter.Item.CLOSE_COMBAT_AS_ITEM_SPELL_ID);
      var _loc3_ = this.getItemFightEffectsText(1);
      if(_loc2_ > -1)
      {
         _loc3_ -= _loc2_;
      }
      return Math.max(1,_loc3_);
   }
   function get rangeMin()
   {
      return this.getItemFightEffectsText(2);
   }
   function get rangeMax()
   {
      return this.getItemFightEffectsText(3);
   }
   function get criticalHit()
   {
      return this.getItemFightEffectsText(4);
   }
   function get criticalFailure()
   {
      return this.getItemFightEffectsText(5);
   }
   function get lineOnly()
   {
      return this.getItemFightEffectsText(6);
   }
   function get lineOfSight()
   {
      return this.getItemFightEffectsText(7);
   }
   function get effectZones()
   {
      return this._aEffectZones;
   }
   function get characteristics()
   {
      var _loc2_ = [];
      _loc2_.push(dofus.datacenter.Item.api.lang.getText("ITEM_AP",[this.apCost]));
      _loc2_.push(dofus.datacenter.Item.api.lang.getText("ITEM_RANGE",[(this.rangeMin == 0 ? "" : this.rangeMin + " " + dofus.datacenter.Item.api.lang.getText("TO_RANGE") + " ") + this.rangeMax]));
      _loc2_.push(dofus.datacenter.Item.api.lang.getText("ITEM_CRITICAL_BONUS",[this.criticalHitBonus <= 0 ? String(this.criticalHitBonus) : "+" + this.criticalHitBonus]));
      _loc2_.push((this.criticalHit == 0 ? "" : dofus.datacenter.Item.api.lang.getText("ITEM_CRITICAL",[this.criticalHit])) + (!(this.criticalHit != 0 && this.criticalFailure != 0) ? "" : " - ") + (this.criticalFailure == 0 ? "" : dofus.datacenter.Item.api.lang.getText("ITEM_MISS",[this.criticalFailure])));
      var _loc3_;
      if(this.criticalHit > 0 && this.ID == dofus.datacenter.Item.api.datacenter.Player.weaponItem.ID)
      {
         _loc3_ = dofus.datacenter.Item.api.kernel.GameManager.getCriticalHitChance(this.criticalHit);
         _loc2_.push(dofus.datacenter.Item.api.lang.getText("ITEM_CRITICAL_REAL",["1/" + _loc3_]));
      }
      return _loc2_;
   }
   function get conditions()
   {
      var _loc2_ = dofus.managers.EffectsManager.parseConditionsString(this._oUnicInfos.c);
      if(this._nRealUnicId != undefined && dofus.datacenter.Item.api.lang.getItemUnicText(this._nRealUnicId).c != undefined)
      {
         _loc2_ = _loc2_.concat(dofus.datacenter.Item.api.lang.getText("INHERITED_FROM",[dofus.datacenter.Item.api.lang.getItemUnicText(this._nRealUnicId).n]) + " :",dofus.managers.EffectsManager.parseConditionsString(dofus.datacenter.Item.api.lang.getItemUnicText(this._nRealUnicId).c));
      }
      return _loc2_;
   }
   function get mood()
   {
      return this._nMood;
   }
   function get skin()
   {
      return this._nSkin;
   }
   function set skin(nSkin)
   {
      this._nSkin = nSkin;
   }
   function get params()
   {
      if(!this.isLeavingItem)
      {
         return undefined;
      }
      var _loc3_ = this.skin;
      if(_loc3_ == undefined || _global.isNaN(_loc3_))
      {
         _loc3_ = 0;
      }
      var _loc4_;
      switch(this.mood)
      {
         case 1:
            _loc4_ = "H";
            break;
         case 2:
         case 0:
            _loc4_ = "U";
            break;
         default:
            _loc4_ = "H";
      }
      return {frame:_loc4_ + _loc3_,forceReload:this.isLeavingItem};
   }
   function get skineable()
   {
      return this._bIsSkineable;
   }
   function get isAssociate()
   {
      return this.skineable && this.realType != 113;
   }
   function get realUnicId()
   {
      if(this._nRealUnicId)
      {
         return this._nRealUnicId;
      }
      return this._nUnicID;
   }
   function get nbSkin()
   {
      return this._nNbSkin;
   }
   function get maxSkin()
   {
      var _loc2_ = this.nbSkin;
      var _loc3_ = 1;
      while(_loc3_ < _loc2_)
      {
         if(this._nLivingXp < dofus.datacenter.Item.LEVEL_STEP[_loc3_])
         {
            return _loc3_;
         }
         _loc3_ += 1;
      }
      return _loc2_;
   }
   function get currentLivingXp()
   {
      return this._nLivingXp;
   }
   function get currentLivingLevelXpMax()
   {
      var _loc2_ = this.nbSkin;
      var _loc3_ = 1;
      while(_loc3_ < _loc2_)
      {
         if(this._nLivingXp < dofus.datacenter.Item.LEVEL_STEP[_loc3_])
         {
            return dofus.datacenter.Item.LEVEL_STEP[_loc3_];
         }
         _loc3_ += 1;
      }
      return -1;
   }
   function get currentLivingLevelXpMin()
   {
      var _loc2_ = this.nbSkin;
      var _loc3_ = 1;
      while(_loc3_ < _loc2_)
      {
         if(this._nLivingXp < dofus.datacenter.Item.LEVEL_STEP[_loc3_])
         {
            return dofus.datacenter.Item.LEVEL_STEP[_loc3_ - 1];
         }
         _loc3_ += 1;
      }
      return -1;
   }
   function get isSpeakingItem()
   {
      return this.isAssociate || this.realType == 113;
   }
   function get isLeavingItem()
   {
      return this.isAssociate || this.realType == 113;
   }
   function get canBeExchange()
   {
      return this._bCanBeExchange;
   }
   function set rideItemDurability(nDurability)
   {
      this._rideItemDurability = nDurability;
   }
   function set rideItemDurabilityMax(nDurability)
   {
      this._rideItemDurabilityMax = nDurability;
   }
   function get rideItemDurability()
   {
      return this._rideItemDurability;
   }
   function get rideItemDurabilityMax()
   {
      return this._rideItemDurabilityMax;
   }
   function get durability()
   {
      return this._durability;
   }
   function get durabilityMax()
   {
      return this._durabilityMax;
   }
   function get skinDurability()
   {
      return this._skinDurability;
   }
   function get skinDurabilityMax()
   {
      return this._skinDurabilityMax;
   }
   function get canBeLock()
   {
      return dofus.Constants.FILTER_EQUIPEMENT[this.superType] && (!this.isLock && !this.isCeremonial);
   }
   function get canBeTemporaryLock()
   {
      return dofus.Constants.FILTER_EQUIPEMENT[this.superType] && (!this._isLock && !this.isCeremonial);
   }
   function get canBeUnlock()
   {
      return dofus.Constants.FILTER_EQUIPEMENT[this.superType] && this._isLock;
   }
   function get isLock()
   {
      return this._isLock || this._isTemporaryLock;
   }
   function initialize(nID, nUnicID, nQuantity, nPosition, sEffects, nPrice, nSkin, nMood)
   {
      dofus.datacenter.Item.api = _global.API;
      this._itemDateId = dofus.datacenter.Item.DATE_ID--;
      this._nID = nID;
      this._nUnicID = nUnicID;
      this._nQuantity = nQuantity != undefined ? nQuantity : 1;
      this._nPosition = nPosition != undefined ? nPosition : -1;
      if(nPrice != undefined)
      {
         this._nPrice = nPrice;
      }
      this._bCanBeExchange = true;
      this._oUnicInfos = dofus.datacenter.Item.api.lang.getItemUnicText(nUnicID);
      this.setEffects(sEffects);
      this._bIsSkineable = false;
      this.updateDataFromEffect();
      var _loc11_ = this.typeText.z;
      var _loc12_ = _loc11_.split("");
      this._aEffectZones = [];
      var _loc13_ = 0;
      while(_loc13_ < _loc12_.length)
      {
         this._aEffectZones.push({shape:_loc12_[_loc13_],size:ank.utils.Compressor.decode64(_loc12_[_loc13_ + 1])});
         _loc13_ += 2;
      }
      this._itemLevel = this.level;
      this._itemType = this.type;
      this._itemPrice = this.price;
      this._itemName = this.name;
      this._itemWeight = this.weight;
      if(nSkin != undefined)
      {
         this._nSkin = nSkin;
      }
      if(nMood != undefined)
      {
         this._nMood = nMood;
      }
   }
   function get isSkinItemCeremonial()
   {
      return this._oSkinItemInfos != undefined && !!this._oSkinItemInfos.ce;
   }
   function get hasCustomSkinItem()
   {
      return this._oSkinItemInfos != undefined && !this._oSkinItemInfos.ce;
   }
   function get hasSkinItem()
   {
      return this._oSkinItemInfos != undefined && this._oSkinItemInfos.ce;
   }
   function get skinItemInfos()
   {
      return this._oSkinItemInfos;
   }
   function setEffects(compressedData)
   {
      this._sEffects = compressedData;
      this._aEffects = [];
      var _loc4_ = compressedData.split(",");
      var _loc5_ = 0;
      var _loc6_;
      while(_loc5_ < _loc4_.length)
      {
         _loc6_ = _loc4_[_loc5_].split("#");
         _loc6_[0] = _global.parseInt(_loc6_[0],16);
         _loc6_[1] = _loc6_[1] != "" ? _global.parseInt(_loc6_[1],16) : undefined;
         _loc6_[2] = _loc6_[2] != "" ? _global.parseInt(_loc6_[2],16) : undefined;
         _loc6_[3] = _loc6_[3] != "" ? _global.parseInt(_loc6_[3],16) : undefined;
         _loc6_[4] = _loc6_[4];
         this._aEffects.push(_loc6_);
         _loc5_ += 1;
      }
   }
   function clone()
   {
      return new dofus.datacenter.Item(this._nID,this._nUnicID,this._nQuantity,this._nPosition,this._sEffects);
   }
   function equals(item)
   {
      return this.unicID == item.unicID;
   }
   function showStatsTooltip(mc, sStyle)
   {
      var _loc4_ = "<b>" + this.name + "</b>" + " - " + dofus.datacenter.Item.api.lang.getText("LEVEL_SMALL") + " " + this.level;
      var _loc5_ = "";
      var _loc6_ = this.visibleEffects;
      var _loc7_;
      for(var _loc8_ in _loc6_)
      {
         _loc7_ = _loc6_[_loc8_].description;
         if(_loc6_[_loc8_].isOver || _loc6_[_loc8_].isExo)
         {
            _loc7_ = "<b>" + _loc7_ + "</b>";
         }
         _loc5_ = "\n" + _loc7_ + _loc5_;
      }
      if(mc._parent._parent._parent._parent._name == "Temporis" || mc._parent._parent._name == "Temporis")
      {
         _loc5_ = _loc5_ + "\n\n" + dofus.datacenter.Item.api.lang.getText("RIGHT_CLICK_ITEM_DETAILS");
      }
      dofus.datacenter.Item.api.ui.showTooltip(_loc4_ + "\n" + _loc5_,mc,0,{bTopAlign:true},sStyle + "ToolTip");
   }
   function getMonsterList()
   {
      var _loc3_ = "";
      var _loc4_ = 0;
      var _loc5_;
      var _loc6_;
      var _loc7_;
      while(_loc4_ < this._aEffects.length)
      {
         _loc5_ = this._aEffects[_loc4_];
         _loc6_ = _loc5_[0];
         _loc7_ = -1;
         if(_loc6_ == dofus.datacenter.Item.OBJECT_ACTION_SUMMON)
         {
            _loc7_ = _global.parseInt(_loc5_[4],dofus.aks.Items.COMPRESSION_RADIX);
         }
         if(_loc6_ == dofus.datacenter.Item.OBJECT_ACTION_SUMMON_RANDOM_GRADE)
         {
            _loc7_ = _global.parseInt(_loc5_[3]);
         }
         if(_loc7_ != -1)
         {
            if(_loc3_.length != 0)
            {
               _loc3_ += "|";
            }
            _loc3_ += _loc7_;
         }
         _loc4_ += 1;
      }
      return _loc3_;
   }
   function getItemFightEffectsText(nPropertyIndex)
   {
      return this._oUnicInfos.e[nPropertyIndex];
   }
   function updateDataFromEffect()
   {
      this._isLock = false;
      this._isTemporaryLock = false;
      var _loc3_;
      var _loc4_;
      for(var _loc5_ in this._aEffects)
      {
         _loc3_ = this._aEffects[_loc5_];
         switch(_loc3_[0])
         {
            case 974:
               this._nLivingXp = !_loc3_[3] ? 0 : _loc3_[3];
               break;
            case 973:
               this._nRealType = !_loc3_[3] ? 0 : _loc3_[3];
               break;
            case 972:
               this._nSkin = !_loc3_[3] ? 0 : _global.parseInt(_loc3_[3]) - 1;
               this._bIsSkineable = true;
               break;
            case 971:
               this._nMood = !_loc3_[3] ? 0 : _loc3_[3];
               break;
            case 969:
               _loc4_ = dofus.datacenter.Item.api.lang.getItemUnicText(!_loc3_[3] ? 0 : _loc3_[3]);
               this._oSkinItemInfos = _loc4_;
               break;
            case 970:
               this._sRealGfx = this._oUnicInfos.g;
               this._sGfx = dofus.datacenter.Item.api.lang.getItemUnicText(!_loc3_[3] ? 0 : _loc3_[3]).g;
               this._nRealUnicId = _loc3_[3];
               break;
            case dofus.datacenter.Item.OBJECT_ACTION_LINK_ACCOUNT:
               this._bCanBeExchange = false;
               break;
            case 975:
               this._nNbSkin = _loc3_[3];
               break;
            case 521:
               this._bUndestroyable = true;
               break;
            case 2149:
               this._skinDurability = !_loc3_[2] ? -1 : Number(_loc3_[2]);
               this._skinDurabilityMax = !_loc3_[3] ? -1 : Number(_loc3_[3]);
               break;
            case 812:
               this._durability = !_loc3_[2] ? -1 : Number(_loc3_[2]);
               this._durabilityMax = !_loc3_[3] ? -1 : Number(_loc3_[3]);
               break;
            case dofus.datacenter.Item.OBJECT_ACTION_INCARNATION:
               this._bIsIncarnation = true;
               break;
            case dofus.datacenter.Item.OBJECT_ACTION_LOCK_TEMPORARY:
               this._isTemporaryLock = true;
               break;
            case dofus.datacenter.Item.OBJECT_ACTION_LOCK:
               this._isLock = true;
         }
      }
      this._bUnknownSkinsCount = this._nNbSkin == undefined;
      if(this._bUnknownSkinsCount)
      {
         this._nNbSkin = dofus.datacenter.Item.LEVEL_STEP.length;
      }
   }
   static function getBaseItemEffects(nItemId)
   {
      var _loc3_ = dofus.datacenter.Item.api.lang.getItemStats(nItemId);
      var _loc4_ = _loc3_.split(",");
      var _loc5_ = [];
      var _loc6_ = 0;
      var _loc7_;
      while(_loc6_ < _loc4_.length)
      {
         _loc7_ = _loc4_[_loc6_].split("#");
         _loc7_[0] = _global.parseInt(_loc7_[0],16);
         _loc7_[1] = _loc7_[1] != "0" ? _global.parseInt(_loc7_[1],16) : undefined;
         _loc7_[2] = _loc7_[2] != "0" ? _global.parseInt(_loc7_[2],16) : undefined;
         _loc7_[3] = _loc7_[3] != "0" ? _global.parseInt(_loc7_[3],16) : undefined;
         _loc5_.push(_loc7_);
         _loc6_ += 1;
      }
      return _loc5_;
   }
   static function canBeExo(nEffectID)
   {
      return nEffectID != dofus.datacenter.Item.OBJECT_ACTION_LINK_CHARACTER && (nEffectID != dofus.datacenter.Item.OBJECT_ACTION_CUSTOM_SKIN && (nEffectID != dofus.datacenter.Item.OBJECT_ACTION_GIVE_AURA && (nEffectID != dofus.datacenter.Item.OBJECT_ACTION_GIVE_TITLE && (nEffectID != dofus.datacenter.Item.OBJECT_ACTION_LEARN_EMOTICON && (nEffectID != dofus.datacenter.Item.OBJECT_ACTION_LINK_ACCOUNT && (nEffectID != dofus.datacenter.Item.OBJECT_ACTION_LAST_MEAL && (nEffectID != dofus.datacenter.Item.OBJECT_ACTION_FM_BY_CHARACTER && (nEffectID != dofus.datacenter.Item.OBJECT_ACTION_CRAFTED_BY_CHARACTER && (nEffectID != dofus.datacenter.Item.OBJECT_ACTION_LOCK && nEffectID != dofus.datacenter.Item.OBJECT_ACTION_LOCK_TEMPORARY)))))))));
   }
   static function isOver(oEffect, nMinValue, nMaxValue)
   {
      for(var _loc4_ in dofus.datacenter.Item.OBJECT_ACTION_SPELL_BOOSTS)
      {
         if(dofus.datacenter.Item.OBJECT_ACTION_SPELL_BOOSTS[_loc4_] == oEffect.type)
         {
            return false;
         }
      }
      if(oEffect.type == dofus.datacenter.Item.OBJECT_ACTION_LAST_MEAL || oEffect.type == dofus.datacenter.Item.OBJECT_ACTION_LINK_ACCOUNT)
      {
         return false;
      }
      if(oEffect.param1 > nMaxValue && oEffect.operator == "+")
      {
         return true;
      }
      if(oEffect.param1 < nMinValue && oEffect.operator == "-")
      {
         return true;
      }
      return false;
   }
   static function getItemDescriptionEffects(aEffects, aBaseEffect, bVisibleOnly, bCanBeExo)
   {
      var _loc6_ = [];
      var _loc7_ = aEffects.length;
      var _loc8_ = {};
      var _loc9_ = new ank.utils.ExtendedArray();
      _loc9_.pushAll(aBaseEffect);
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
      var _loc20_;
      var _loc21_;
      var _loc22_;
      var _loc23_;
      var _loc24_;
      var _loc25_;
      var _loc26_;
      var _loc27_;
      var _loc28_;
      var _loc29_;
      if(typeof aEffects == "object")
      {
         _loc10_ = 0;
         for(; _loc10_ < _loc7_; _loc10_ += 1)
         {
            _loc11_ = aEffects[_loc10_];
            _loc12_ = _loc11_[0];
            _loc13_ = [];
            switch(_loc12_)
            {
               case dofus.datacenter.Item.OBJECT_ACTION_SUMMON:
                  _loc14_ = _loc11_[4].split(dofus.aks.Items.EFFECT_APPEND_CHAR);
                  _loc15_ = 0;
                  while(_loc15_ < _loc14_.length)
                  {
                     _loc16_ = _global.parseInt(_loc14_[_loc15_],dofus.aks.Items.COMPRESSION_RADIX);
                     _loc17_ = new dofus.datacenter.Effect(undefined,_loc12_,undefined,undefined,_loc16_);
                     _loc13_.push(_loc17_);
                     _loc15_ += 1;
                  }
                  break;
               case dofus.datacenter.Item.OBJECT_ACTION_REPLACE:
                  _loc18_ = dofus.datacenter.Item.getBaseItemEffects(_loc11_[3]);
                  _loc19_ = 0;
                  while(_loc19_ < _loc18_.length)
                  {
                     _loc20_ = _loc18_[_loc19_];
                     _loc21_ = new dofus.datacenter.Effect(undefined,_loc20_[0],_loc20_[1],_loc20_[2],_loc20_[3]);
                     _loc13_.push(_loc21_);
                     _loc19_ += 1;
                  }
                  break;
               default:
                  _loc22_ = new dofus.datacenter.Effect(undefined,_loc12_,_loc11_[1],_loc11_[2],_loc11_[3],_loc11_[4]);
                  if(!(_loc22_.operator == "-" && _loc22_.param1 == undefined))
                  {
                     _loc8_[_loc12_] = _loc8_[_loc12_] != undefined ? _loc8_[_loc12_] + 1 : 0;
                     _loc23_ = bCanBeExo && dofus.datacenter.Item.canBeExo(_loc12_);
                     _loc24_ = false;
                     if(_loc9_ != undefined && _loc9_.length > 0)
                     {
                        _loc25_ = 0;
                        while(_loc25_ < _loc9_.length)
                        {
                           if(_loc9_[_loc25_][0] == _loc12_)
                           {
                              _loc23_ = false;
                              _loc26_ = _loc9_[_loc25_][1];
                              _loc27_ = _loc9_[_loc25_][2];
                              _loc24_ = dofus.datacenter.Item.isOver(_loc22_,_loc26_,!_global.isNaN(_loc27_) ? _loc27_ : _loc26_);
                              _loc9_.splice(_loc25_,1);
                              break;
                           }
                           _loc25_ += 1;
                        }
                     }
                     _loc22_.isOver = _loc24_;
                     _loc22_.isExo = _loc23_;
                     _loc13_.push(_loc22_);
                     break;
                  }
                  continue;
            }
            _loc28_ = 0;
            while(_loc28_ < _loc13_.length)
            {
               _loc29_ = _loc13_[_loc28_];
               if(bVisibleOnly == true)
               {
                  if(_loc29_.showInTooltip)
                  {
                     _loc6_.push(_loc29_);
                  }
               }
               else
               {
                  _loc6_.push(_loc29_);
               }
               _loc28_ += 1;
            }
         }
         return _loc6_;
      }
      return null;
   }
   function addGlowOnItemIcon(mcGfx, nColor, nAlpha, nBlur, nIntensity)
   {
      mcGfx.filters = [new flash.filters.GlowFilter(nColor,nAlpha,nBlur,nBlur,nIntensity,1,false,false)];
   }
}
