class dofus.aks.Exchange extends dofus.aks.Handler
{
   var _lastMovedItemAdd;
   var _nItemsToCraft;
   var aks;
   var api;
   var _lastMovedItemId = -1;
   function Exchange(oAKS, oAPI)
   {
      super.initialize(oAKS,oAPI);
   }
   function resetLastMovedItem()
   {
      this._lastMovedItemId = -1;
   }
   function leave()
   {
      this.aks.send("EV",true);
   }
   function request(type, id, cellNum)
   {
      this.aks.send("ER" + type + "|" + (!(id == undefined || _global.isNaN(id)) ? id : "") + (!(cellNum == undefined || _global.isNaN(cellNum)) ? "|" + cellNum : ""),true);
   }
   function shop(nID)
   {
      this.aks.send("Es" + nID);
   }
   function accept()
   {
      this.aks.send("EA",false);
   }
   function movementOgrine(_loc2_)
   {
      this.aks.send("EMS" + _loc2_,true);
   }
   function ready()
   {
      this.aks.send("EK",true);
   }
   function movementItem(bAdd, oItem, nQuantity, nPrice)
   {
      if(this._lastMovedItemId == oItem.ID && bAdd == this._lastMovedItemAdd)
      {
         return undefined;
      }
      if(nQuantity == oItem.Quantity)
      {
         this._lastMovedItemId = oItem.ID;
         this._lastMovedItemAdd = bAdd;
      }
      else
      {
         this._lastMovedItemId = -1;
      }
      var _loc6_ = "EMO" + (!bAdd ? "-" : "+") + oItem.ID + "|" + nQuantity + (nPrice != undefined ? "|" + nPrice : "");
      this.aks.send(_loc6_,true);
   }
   function movementItems(aItems)
   {
      var _loc3_ = "";
      var _loc4_ = 0;
      var _loc5_;
      var _loc6_;
      var _loc7_;
      var _loc8_;
      while(_loc4_ < aItems.length)
      {
         _loc5_ = aItems[_loc4_].Add;
         _loc6_ = aItems[_loc4_].ID;
         _loc7_ = aItems[_loc4_].Quantity;
         _loc8_ = aItems[_loc4_].Price;
         _loc3_ += (!_loc5_ ? "-" : "+") + _loc6_ + "|" + _loc7_ + (_loc8_ != undefined ? "|" + _loc8_ : "");
         _loc4_ += 1;
      }
      this.aks.send("EMO" + _loc3_,true,undefined,true);
   }
   function movementPayItem(nGarbage, bAdd, nID, nQuantity, nPrice)
   {
      this.aks.send("EP" + nGarbage + "O" + (!bAdd ? "-" : "+") + nID + "|" + nQuantity + (nPrice != undefined ? "|" + nPrice : ""),true);
   }
   function movementKama(nQuantity)
   {
      this.aks.send("EMG" + nQuantity,true);
   }
   function movementPayKama(nGarbage, nQuantity)
   {
      this.aks.send("EP" + nGarbage + "G" + nQuantity,true);
   }
   function sell(id, quantity)
   {
      this.aks.send("ES" + id + "|" + quantity,true);
   }
   function buy(nID, nQuantity)
   {
      this.aks.send("EB" + nID + "|" + nQuantity,true);
   }
   function offlineExchange()
   {
      this.aks.send("EQ",true);
   }
   function askOfflineExchange()
   {
      this.aks.send("Eq",true);
   }
   function bigStoreType(nTypeID)
   {
      this.aks.send("EHT" + nTypeID);
   }
   function bigStoreItemList(nUnicID)
   {
      this.aks.send("EHl" + nUnicID);
   }
   function bigStoreSoulList(sMonstersList)
   {
      this.aks.send("EHM" + sMonstersList);
   }
   function bigStoreBuy(nID, nQuantityIndex, nPrice)
   {
      this.aks.send("EHB" + nID + "|" + nQuantityIndex + "|" + nPrice,true);
   }
   function bigStoreSearch(nType, nUnicID)
   {
      this.aks.send("EHS" + nType + "|" + nUnicID);
   }
   function setPublicMode(b)
   {
      this.aks.send("EW" + (!b ? "-" : "+"),false);
   }
   function getCrafterForJob(nJobId)
   {
      this.aks.send("EJF" + nJobId,true);
   }
   function putInShedFromInventory(nMountID)
   {
      this.aks.send("Erp" + nMountID,true);
   }
   function putInInventoryFromShed(nMountID)
   {
      this.aks.send("Erg" + nMountID,true);
   }
   function putInCertificateFromShed(nMountID)
   {
      this.aks.send("Erc" + nMountID,true);
   }
   function putInShedFromCertificate(nCertifID)
   {
      this.aks.send("ErC" + nCertifID,true);
   }
   function putInMountParkFromShed(nMountID)
   {
      this.aks.send("Efp" + nMountID,true);
   }
   function putInShedFromMountPark(nMountID)
   {
      this.aks.send("Efg" + nMountID,true);
   }
   function killMountInPark(nMountID)
   {
      this.aks.send("Eff" + nMountID,false);
   }
   function killMount(nMountID)
   {
      this.aks.send("Erf" + nMountID,false);
   }
   function getItemMiddlePriceInBigStore(nItemID)
   {
      this.aks.send("EHP" + nItemID,false);
   }
   function replayCraft()
   {
      this.aks.send("EL",false);
   }
   function repeatCraft(nHowManyTimes)
   {
      this._nItemsToCraft = nHowManyTimes;
      this.aks.send("EMR" + nHowManyTimes,false);
      this.api.datacenter.Basics.isCraftLooping = true;
   }
   function stopRepeatCraft()
   {
      this.aks.send("EMr",false);
   }
   function onRequest(bSuccess, sExtraData)
   {
      var _loc4_;
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
      if(bSuccess)
      {
         _loc4_ = sExtraData.split("|");
         _loc5_ = _loc4_[0];
         _loc6_ = _loc4_[1];
         _loc7_ = Number(_loc4_[2]);
         _loc8_ = this.api.datacenter.Player.ID != _loc5_ ? _loc5_ : _loc6_;
         if(_loc7_ == 12 || _loc7_ == 13)
         {
            _loc9_ = new dofus.datacenter.SecureCraftExchange(_loc8_);
         }
         else
         {
            _loc9_ = new dofus.datacenter.Exchange(_loc8_);
         }
         this.api.datacenter.Exchange = _loc9_;
         if(this.api.datacenter.Player.ID == _loc5_)
         {
            _loc10_ = this.api.datacenter.Sprites.getItemAt(_loc6_);
            switch(_loc7_)
            {
               case 1:
                  _loc11_ = "WAIT_FOR_EXCHANGE";
                  break;
               case 12:
                  _loc11_ = "WAIT_FOR_CRAFT_CLIENT";
                  break;
               case 13:
                  _loc11_ = "WAIT_FOR_CRAFT_ARTISAN";
            }
            this.api.kernel.showMessage(this.api.lang.getText("EXCHANGE"),this.api.lang.getText(_loc11_,[_loc10_.name]),"INFO_CANCEL",{name:"Exchange",listener:this});
         }
         else
         {
            _loc12_ = this.api.datacenter.Sprites.getItemAt(_loc5_);
            if(this.api.kernel.ChatManager.isBlacklisted(_loc12_.name))
            {
               this.leave();
               return undefined;
            }
            this.api.kernel.showMessage(undefined,this.api.lang.getText("CHAT_A_WANT_EXCHANGE",[this.api.kernel.ChatManager.getLinkName(_loc12_.id,_loc12_.name)]),"INFO_CHAT");
            switch(_loc7_)
            {
               case 1:
                  _loc13_ = "A_WANT_EXCHANGE";
                  break;
               case 12:
                  _loc13_ = "A_WANT_CRAFT_CLIENT";
                  break;
               case 13:
                  _loc13_ = "A_WANT_CRAFT_ARTISAN";
            }
            this.api.electron.makeNotification(this.api.lang.getText(_loc13_,[_loc12_.name]));
            this.api.kernel.showMessage(this.api.lang.getText("EXCHANGE"),this.api.lang.getText(_loc13_,[_loc12_.name]),"CAUTION_YESNOIGNORE",{name:"Exchange",player:_loc12_.name,listener:this,params:{player:_loc12_.name}});
         }
      }
      _loc14_ = sExtraData.charAt(0);
      switch(_loc14_)
      {
         case "O":
            this.api.kernel.showMessage(undefined,this.api.lang.getText("ALREADY_EXCHANGE"),"ERROR_CHAT");
            return undefined;
         case "T":
            this.api.kernel.showMessage(undefined,this.api.lang.getText("NOT_NEAR_CRAFT_TABLE"),"ERROR_CHAT");
            return undefined;
         case "J":
            this.api.kernel.showMessage(undefined,this.api.lang.getText("ERROR_85"),"ERROR_CHAT");
            return undefined;
         case "o":
            this.api.kernel.showMessage(undefined,this.api.lang.getText("ERROR_70"),"ERROR_CHAT");
            return undefined;
         case "S":
            this.api.kernel.showMessage(undefined,this.api.lang.getText("ERROR_62"),"ERROR_CHAT");
            return undefined;
         case "I":
         default:
            this.api.kernel.showMessage(undefined,this.api.lang.getText("CANT_EXCHANGE"),"ERROR_CHAT");
            return undefined;
      }
   }
   function onAskOfflineExchange(sExtraData)
   {
      var _loc3_ = sExtraData.split("|");
      var _loc4_ = Number(_loc3_[0]);
      var _loc5_ = Number(_loc3_[1]) / 10;
      var _loc6_ = Number(_loc3_[2]);
      this.api.kernel.GameManager.askOfflineExchange(_loc4_,_loc5_,_loc6_);
   }
   function onReady(sExtraData)
   {
      var _loc3_ = sExtraData.charAt(0) == "1";
      var _loc4_ = Number(sExtraData.substr(1));
      var _loc5_ = _loc4_ != this.api.datacenter.Player.ID ? 1 : 0;
      this.api.datacenter.Exchange.readyStates.updateItem(_loc5_,_loc3_);
   }
   function onLeave(bSuccess, sExtraData)
   {
      delete this.api.datacenter.Basics.aks_exchange_echangeType;
      delete this.api.datacenter.Exchange;
      this.api.ui.unloadUIComponent("AskYesNoIgnoreExchange");
      this.api.ui.unloadUIComponent("AskCancelExchange");
      if(this.api.ui.getUIComponent("Exchange"))
      {
         if(sExtraData == "a")
         {
            this.api.kernel.showMessage(undefined,this.api.lang.getText("EXCHANGE_OK"),"INFO_CHAT");
         }
         else
         {
            this.api.kernel.showMessage(undefined,this.api.lang.getText("EXCHANGE_CANCEL"),"INFO_CHAT");
         }
      }
      this.api.ui.unloadUIComponent("Exchange");
      this.api.ui.unloadUIComponent("Craft");
      this.api.ui.unloadUIComponent("NpcShop");
      this.api.ui.unloadUIComponent("PlayerShop");
      this.api.ui.unloadUIComponent("TaxCollectorStorage");
      this.api.ui.unloadUIComponent("PlayerShopModifier");
      this.api.ui.unloadUIComponent("Storage");
      this.api.ui.unloadUIComponent("BigStoreSell");
      this.api.ui.unloadUIComponent("BigStoreBuy");
      this.api.ui.unloadUIComponent("SecureCraft");
      this.api.ui.unloadUIComponent("CrafterList");
      this.api.ui.unloadUIComponent("ItemUtility");
      this.api.ui.unloadUIComponent("MountStorage");
      this.api.ui.unloadUIComponent("MountParkSale");
      this.api.ui.unloadUIComponent("HouseSale");
      this.api.ui.unloadUIComponent("CardsRecycler");
      this.api.ui.unloadUIComponent("CardsUpgrader");
      if(dofus.Constants.SAVING_THE_WORLD)
      {
         dofus.SaveTheWorld.getInstance().nextAction();
      }
   }
   function onCreate(bSuccess, sExtraData)
   {
      if(!bSuccess)
      {
         return undefined;
      }
      this._lastMovedItemId = -1;
      var _loc5_ = sExtraData.split("|");
      var _loc6_ = Number(_loc5_[0]);
      var _loc7_ = _loc5_[1];
      this.api.datacenter.Basics.aks_exchange_echangeType = _loc6_;
      var _loc8_ = this.api.datacenter.Temporary;
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
      var _loc20_;
      var _loc21_;
      var _loc22_;
      var _loc23_;
      var _loc24_;
      var _loc25_;
      var _loc26_;
      var _loc27_;
      switch(_loc6_)
      {
         case 0:
         case 4:
            _loc8_.Shop = new dofus.datacenter.Shop();
            _loc9_ = this.api.datacenter.Sprites.getItemAt(_loc7_);
            _loc8_.Shop.id = _loc9_.id;
            _loc8_.Shop.name = _loc9_.name;
            _loc8_.Shop.gfx = _loc9_.gfxID;
            _loc10_ = [];
            _loc10_[1] = _loc9_.color1 != undefined ? _loc9_.color1 : -1;
            _loc10_[2] = _loc9_.color2 != undefined ? _loc9_.color2 : -1;
            _loc10_[3] = _loc9_.color3 != undefined ? _loc9_.color3 : -1;
            if(_loc6_ == 0)
            {
               this.api.ui.loadUIComponent("NpcShop","NpcShop",{data:_loc8_.Shop,colors:_loc10_});
               return undefined;
            }
            if(_loc6_ == 4)
            {
               _loc8_.Shop.characterID = _loc9_.characterID;
               this.api.ui.loadUIComponent("PlayerShop","PlayerShop",{data:_loc8_.Shop,colors:_loc10_});
               return undefined;
            }
            return undefined;
            break;
         case 1:
            this.api.datacenter.Exchange.inventory = this.api.datacenter.Player.Inventory.deepClone();
            this.api.ui.unloadUIComponent("AskYesNoIgnoreExchange");
            this.api.ui.unloadUIComponent("AskCancelExchange");
            this.api.ui.loadUIComponent("Exchange","Exchange");
            return undefined;
         case 2:
         case 9:
         case 17:
         case 18:
         case 3:
            if(_loc6_ == 3)
            {
               this.api.datacenter.Exchange = new dofus.datacenter.Exchange();
            }
            else
            {
               this.api.datacenter.Exchange = new dofus.datacenter.Exchange(Number(_loc7_));
            }
            this.api.datacenter.Exchange.inventory = this.api.datacenter.Player.Inventory.deepClone();
            if(_loc6_ == 3)
            {
               _loc5_ = _loc7_.split(";");
               _loc11_ = Number(_loc5_[0]);
               _loc12_ = Number(_loc5_[1]);
               if(_global.API.lang.getSkillForgemagus(_loc12_) > 0)
               {
                  this.api.ui.loadUIComponent("ForgemagusCraftNew","Craft",{skillId:_loc12_,maxItem:_loc11_},{nHideSprites:1});
                  return undefined;
               }
               if(_loc12_ == 181)
               {
                  this.api.ui.loadUIComponent("Decraft","Craft",{skillId:_loc12_,maxItem:_loc11_});
                  return undefined;
               }
               this.api.ui.loadUIComponent("Craft","Craft",{skillId:_loc12_,maxItem:_loc11_});
               return undefined;
            }
            this.api.ui.unloadUIComponent("AskYesNoIgnoreExchange");
            this.api.ui.unloadUIComponent("AskCancelExchange");
            this.api.ui.loadUIComponent("Exchange","Exchange");
            return undefined;
            break;
         case 5:
            _loc8_.Storage = new dofus.datacenter.Storage();
            this.api.ui.loadUIComponent("Storage","Storage",{data:_loc8_.Storage},{nHideSprites:1});
            return undefined;
         case 8:
            _loc8_.Storage = new dofus.datacenter.TaxCollectorStorage();
            _loc13_ = this.api.datacenter.Sprites.getItemAt(_loc7_);
            _loc8_.Storage.name = _loc13_.name;
            _loc8_.Storage.gfx = _loc13_.gfxID;
            this.api.ui.loadUIComponent("TaxCollectorStorage","TaxCollectorStorage",{data:_loc8_.Storage});
            return undefined;
         case 6:
            _loc8_.Shop = new dofus.datacenter.Shop();
            this.api.ui.loadUIComponent("PlayerShopModifier","PlayerShopModifier",{data:_loc8_.Shop});
            return undefined;
         case 10:
            _loc8_.Shop = new dofus.datacenter.BigStore();
            _loc5_ = _loc7_.split(";");
            _loc14_ = _loc5_[0].split(",");
            _loc8_.Shop.quantity1 = Number(_loc14_[0]);
            _loc8_.Shop.quantity2 = Number(_loc14_[1]);
            _loc8_.Shop.quantity3 = Number(_loc14_[2]);
            _loc8_.Shop.types = _loc5_[1].split(",");
            _loc8_.Shop.tax = Number(_loc5_[2]);
            _loc8_.Shop.maxLevel = Number(_loc5_[3]);
            _loc8_.Shop.maxItemCount = Number(_loc5_[4]);
            _loc8_.Shop.npcID = Number(_loc5_[5]);
            _loc8_.Shop.maxSellTime = Number(_loc5_[6]);
            this.api.ui.loadUIComponent("BigStoreSell","BigStoreSell",{data:_loc8_.Shop},{nHideSprites:1});
            return undefined;
         case 11:
            _loc8_.Shop = new dofus.datacenter.BigStore();
            _loc5_ = _loc7_.split(";");
            _loc15_ = _loc5_[0].split(",");
            _loc8_.Shop.quantity1 = Number(_loc15_[0]);
            _loc8_.Shop.quantity2 = Number(_loc15_[1]);
            _loc8_.Shop.quantity3 = Number(_loc15_[2]);
            _loc8_.Shop.types = _loc5_[1].split(",");
            _loc8_.Shop.tax = Number(_loc5_[2]);
            _loc8_.Shop.maxLevel = Number(_loc5_[3]);
            _loc8_.Shop.maxItemCount = Number(_loc5_[4]);
            _loc8_.Shop.npcID = Number(_loc5_[5]);
            _loc8_.Shop.maxSellTime = Number(_loc5_[6]);
            this.api.ui.loadUIComponent("BigStoreBuy","BigStoreBuy",{data:_loc8_.Shop},{nHideSprites:1});
            return undefined;
         case 12:
         case 13:
            this.api.datacenter.Exchange.inventory = this.api.datacenter.Player.Inventory.deepClone();
            _loc5_ = _loc7_.split(";");
            _loc16_ = Number(_loc5_[0]);
            _loc17_ = Number(_loc5_[1]);
            this.api.ui.unloadUIComponent("AskYesNoIgnoreExchange");
            this.api.ui.unloadUIComponent("AskCancelExchange");
            this.api.ui.loadUIComponent("SecureCraft","SecureCraft",{skillId:_loc17_,maxItem:_loc16_});
            if(!dofus.graphics.gapi.ui.SecureCraft.secureCraftNotified)
            {
               this.api.kernel.showMessage(undefined,this.api.lang.getText("SECURECRAFT_NOTIFICATION"),"ERROR_CHAT");
               dofus.graphics.gapi.ui.SecureCraft.secureCraftNotified = true;
               return undefined;
            }
            return undefined;
            break;
         case 14:
            _loc18_ = new ank.utils.ExtendedArray();
            _loc19_ = _loc7_.split(";");
            _loc20_ = 0;
            while(_loc20_ < _loc19_.length)
            {
               _loc21_ = Number(_loc19_[_loc20_]);
               _loc18_.push({label:this.api.lang.getJobText(_loc21_).n,id:_loc21_});
               _loc20_ += 1;
            }
            this.api.ui.loadUIComponent("CrafterList","CrafterList",{crafters:new ank.utils.ExtendedArray(),jobs:_loc18_});
            return undefined;
         case 15:
            this.api.ui.unloadUIComponent("Mount");
            _loc8_.Storage = new dofus.datacenter.Storage();
            this.api.ui.loadUIComponent("Storage","Storage",{isMount:true,data:_loc8_.Storage});
            return undefined;
         case 16:
            _loc22_ = new ank.utils.ExtendedArray();
            _loc23_ = new ank.utils.ExtendedArray();
            _loc5_ = _loc7_.split("~");
            _loc24_ = _loc5_[0].split(";");
            _loc25_ = _loc5_[1].split(";");
            if(_loc24_ != undefined)
            {
               _loc26_ = 0;
               while(_loc26_ < _loc24_.length)
               {
                  if(_loc24_[_loc26_] != "")
                  {
                     _loc22_.push(this.api.network.Mount.createMount(_loc24_[_loc26_]));
                  }
                  _loc26_ += 1;
               }
            }
            if(_loc25_ != undefined)
            {
               _loc27_ = 0;
               while(_loc27_ < _loc25_.length)
               {
                  if(_loc25_[_loc27_] != "")
                  {
                     _loc23_.push(this.api.network.Mount.createMount(_loc25_[_loc27_]));
                  }
                  _loc27_ += 1;
               }
            }
            this.api.ui.loadUIComponent("MountStorage","MountStorage",{mounts:_loc22_,parkMounts:_loc23_});
      }
      return undefined;
   }
   function onCrafterReference(sExtraData)
   {
      var _loc3_ = sExtraData.charAt(0) == "+";
      var _loc4_ = Number(sExtraData.substr(1));
      this.api.kernel.showMessage(undefined,this.api.lang.getText(!_loc3_ ? "CRAFTER_REFERENCE_REMOVE" : "CRAFTER_REFERENCE_ADD",[this.api.lang.getJobText(_loc4_).n]),"INFO_CHAT");
   }
   function onCrafterListChanged(sExtraData)
   {
      var _loc3_ = sExtraData.charAt(0) == "+";
      var _loc4_ = sExtraData.substr(1).split(";");
      var _loc5_ = this.api.ui.getUIComponent("CrafterList").crafters;
      var _loc6_ = Number(_loc4_[0]);
      var _loc7_ = _loc4_[1];
      var _loc8_ = _loc5_.findFirstItem("id",_loc7_);
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
      if(_loc3_)
      {
         _loc9_ = _loc4_[2];
         _loc10_ = Number(_loc4_[3]);
         _loc11_ = Number(_loc4_[4]);
         _loc12_ = !!Number(_loc4_[5]);
         _loc13_ = Number(_loc4_[6]);
         _loc14_ = Number(_loc4_[7]);
         _loc15_ = _loc4_[8].split(",");
         _loc16_ = _loc4_[9];
         _loc17_ = _loc4_[10].split(",");
         _loc18_ = new dofus.datacenter.Crafter(_loc7_,_loc9_);
         _loc18_.job = new dofus.datacenter.Job(_loc6_,new ank.utils.ExtendedArray(),new dofus.datacenter.JobOptions(Number(_loc17_[0]),Number(_loc17_[1])));
         _loc18_.job.level = _loc10_;
         _loc18_.mapId = _loc11_;
         _loc18_.inWorkshop = _loc12_;
         _loc18_.breedId = _loc13_;
         _loc18_.sex = _loc14_;
         _loc18_.color1 = _loc15_[0];
         _loc18_.color2 = _loc15_[1];
         _loc18_.color3 = _loc15_[2];
         this.api.kernel.CharactersManager.setSpriteAccessories(_loc18_,_loc16_);
         if(_loc8_.index != -1)
         {
            _loc5_.updateItem(_loc8_.index,_loc18_);
         }
         else
         {
            _loc5_.push(_loc18_);
         }
      }
      else if(_loc8_.index != -1)
      {
         _loc5_.removeItems(_loc8_.index,1);
      }
   }
   function onMountStorage(sExtraData)
   {
      var _loc3_ = sExtraData.charAt(0);
      var _loc4_ = false;
      var _loc5_;
      var _loc6_;
      switch(_loc3_)
      {
         case "~":
            _loc4_ = true;
            break;
         case "+":
            break;
         case "-":
            _loc5_ = Number(sExtraData.substr(1));
            _loc6_ = this.api.ui.getUIComponent("MountStorage").mounts;
            for(var _loc7_ in _loc6_)
            {
               if(_loc6_[_loc7_].ID == _loc5_)
               {
                  _loc6_.removeItems(Number(_loc7_),1);
               }
            }
            return undefined;
         case "E":
         default:
            return undefined;
      }
      this.api.ui.getUIComponent("MountStorage").mounts.push(this.api.network.Mount.createMount(sExtraData.substr(1),_loc4_));
   }
   function onMountPark(sExtraData)
   {
      var _loc3_ = sExtraData.charAt(0);
      var _loc4_;
      var _loc5_;
      var _loc6_;
      switch(_loc3_)
      {
         case "+":
            this.api.ui.getUIComponent("MountStorage").parkMounts.push(this.api.network.Mount.createMount(sExtraData.substr(1)));
            break;
         case "-":
            _loc4_ = Number(sExtraData.substr(1));
            _loc5_ = this.api.ui.getUIComponent("MountStorage").parkMounts;
            for(_loc6_ in _loc5_)
            {
               if(_loc5_[_loc6_].ID == _loc4_)
               {
                  _loc5_.removeItems(Number(_loc6_),1);
                  return undefined;
               }
            }
         case "E":
         default:
            return;
      }
   }
   function onCraft(bSuccess, sExtraData)
   {
      this._lastMovedItemId = -1;
      if(this.api.datacenter.Basics.aks_exchange_isForgemagus || !this.api.datacenter.Basics.isCraftLooping)
      {
         this.api.datacenter.Exchange.clearLocalGarbage();
      }
      var _loc4_ = this.api.datacenter.Basics.aks_exchange_echangeType;
      var _loc5_;
      if(_loc4_ == 12 || _loc4_ == 13)
      {
         _loc5_ = this.api.datacenter.Exchange;
         _loc5_.clearDistantGarbage();
         _loc5_.clearPayGarbage();
         _loc5_.clearPayIfSuccessGarbage();
         _loc5_.payKama = 0;
         _loc5_.payIfSuccessKama = 0;
         this.api.ui.getUIComponent("SecureCraft").updateInventory();
      }
      var _loc6_ = !this.api.datacenter.Basics.aks_exchange_isForgemagus;
      var _loc7_;
      var _loc8_;
      var _loc9_;
      var _loc10_;
      var _loc11_;
      var _loc12_;
      var _loc13_;
      var _loc14_ = sExtraData.substr(0,1);
      switch(_loc14_)
      {
         case "I":
            if(!bSuccess)
            {
               this.api.kernel.showMessage(this.api.lang.getText("CRAFT"),this.api.lang.getText("NO_CRAFT_RESULT"),"ERROR_BOX",{name:"Impossible"});
            }
            break;
         case "F":
            if(!bSuccess && _loc6_)
            {
               this.api.kernel.showMessage(this.api.lang.getText("CRAFT"),this.api.lang.getText("CRAFT_FAILED"),"ERROR_BOX",{name:"CraftFailed"});
            }
            this.api.kernel.SpeakingItemsManager.triggerEvent(dofus.managers.SpeakingItemsManager.SPEAK_TRIGGER_CRAFT_KO);
            break;
         case ";":
            if(!bSuccess)
            {
               break;
            }
            _loc7_ = sExtraData.substr(1).split(";");
            if(_loc7_.length == 1)
            {
               _loc8_ = new dofus.datacenter.Item(0,Number(_loc7_[0]),undefined,undefined,undefined);
               this.api.kernel.showMessage(undefined,this.api.lang.getText("CRAFT_SUCCESS_SELF",[_loc8_.name]),"INFO_CHAT");
               this.api.kernel.SpeakingItemsManager.triggerEvent(dofus.managers.SpeakingItemsManager.SPEAK_TRIGGER_CRAFT_KO);
               break;
            }
            _loc9_ = _loc7_[1].substr(0,1);
            _loc10_ = _loc7_[1].substr(1);
            _loc11_ = Number(_loc7_[0]);
            _loc12_ = _loc7_[2];
            _loc13_ = [];
            _loc13_.push(_loc11_);
            _loc13_.push(_loc12_);
            switch(_loc9_)
            {
               case "T":
                  this.api.kernel.showMessage(undefined,this.api.kernel.ChatManager.parseInlineItems(this.api.lang.getText("CRAFT_SUCCESS_TARGET",[_loc10_]),_loc13_),"INFO_CHAT");
                  break;
               case "B":
                  this.api.kernel.showMessage(undefined,this.api.kernel.ChatManager.parseInlineItems(this.api.lang.getText("CRAFT_SUCCESS_OTHER",[_loc10_]),_loc13_),"INFO_CHAT");
            }
      }
      if(!bSuccess)
      {
         this.api.datacenter.Exchange.clearCoopGarbage();
      }
   }
   function onCraftLoop(sExtraData)
   {
      var _loc3_ = Number(sExtraData);
      this.api.kernel.showMessage(undefined,this.api.lang.getText("CRAFT_LOOP_PROCESS",[this._nItemsToCraft - _loc3_ + 1,this._nItemsToCraft + 1]),"INFO_CHAT");
   }
   function onCraftLoopEnd(sExtraData)
   {
      var _loc3_ = Number(sExtraData);
      this.api.datacenter.Basics.isCraftLooping = false;
      var _loc4_;
      switch(_loc3_)
      {
         case 1:
            this.api.electron.makeNotification(this.api.lang.getText("CRAFT_LOOP_END_OK"));
            _loc4_ = this.api.lang.getText("CRAFT_LOOP_END_OK");
            break;
         case 2:
            _loc4_ = this.api.lang.getText("CRAFT_LOOP_END_INTERRUPT");
            break;
         case 3:
            _loc4_ = this.api.lang.getText("CRAFT_LOOP_END_FAIL");
            break;
         case 4:
            _loc4_ = this.api.lang.getText("CRAFT_LOOP_END_INVALID");
      }
      this.api.kernel.showMessage(undefined,_loc4_,"INFO_CHAT");
      this.api.kernel.showMessage(this.api.lang.getText("CRAFT"),_loc4_,"ERROR_BOX");
      this.api.ui.getUIComponent("Craft").onCraftLoopEnd();
      if(!this.api.datacenter.Basics.aks_exchange_isForgemagus)
      {
         this.api.datacenter.Exchange.clearLocalGarbage();
      }
   }
   function onLocalMovement(bSuccess, sExtraData)
   {
      this._lastMovedItemId = -1;
      this.modifyLocal(sExtraData,this.api.datacenter.Exchange.localGarbage,"localKama","localOgrine");
   }
   function onDistantMovement(bSuccess, sExtraData)
   {
      var _loc4_;
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
      switch(this.api.datacenter.Basics.aks_exchange_echangeType)
      {
         case 1:
         case 2:
         case 3:
         case 9:
         case 12:
         case 13:
            this.modifyDistant(sExtraData,this.api.datacenter.Exchange.distantGarbage,"distantKama",undefined,"distantOgrine");
            return undefined;
         case 10:
            _loc4_ = sExtraData.charAt(0) == "+";
            _loc5_ = sExtraData.substr(1).split("|");
            _loc6_ = Number(_loc5_[0]);
            _loc7_ = Number(_loc5_[1]);
            _loc8_ = Number(_loc5_[2]);
            _loc9_ = _loc5_[3];
            _loc10_ = Number(_loc5_[4]);
            _loc11_ = Number(_loc5_[5]);
            _loc12_ = this.api.datacenter.Temporary.Shop;
            _loc13_ = _loc12_.inventory.findFirstItem("ID",_loc6_);
            _loc14_ = dofus.graphics.gapi.ui.BigStoreSell(this.api.ui.getUIComponent("BigStoreSell"));
            if(_loc4_)
            {
               _loc15_ = new dofus.datacenter.Item(_loc6_,_loc8_,_loc7_,-1,_loc9_,_loc10_);
               _loc15_.remainingHours = _loc11_;
               if(_loc13_.index != -1)
               {
                  _loc12_.inventory.updateItem(_loc13_.index,_loc15_);
                  _loc14_.updateTotalKamas(_loc13_.item.price - _loc10_,true);
               }
               else
               {
                  _loc12_.inventory.push(_loc15_);
                  _loc14_.updateTotalKamas(_loc10_,true);
               }
            }
            else if(_loc13_.index != -1)
            {
               _loc12_.inventory.removeItems(_loc13_.index,1);
               _loc14_.updateTotalKamas(_loc13_.item.price);
            }
            else
            {
               ank.utils.Logger.err("[onDistantMovement] cet objet n\'existe pas id=" + _loc6_);
            }
            if(_loc14_ != undefined)
            {
               _loc14_.updateItemCount();
               _loc14_.refreshRemoveButton();
            }
      }
      return undefined;
   }
   function onCoopMovement(bSuccess, sExtraData)
   {
      this.api.datacenter.Exchange.clearCoopGarbage();
      switch(this.api.datacenter.Basics.aks_exchange_echangeType)
      {
         case 12:
            this.modifyDistant(sExtraData,this.api.datacenter.Exchange.coopGarbage,"distantKama",false);
            return undefined;
         case 13:
            this.modifyDistant(sExtraData,this.api.datacenter.Exchange.coopGarbage,"distantKama",true);
      }
      return undefined;
   }
   function onPayMovement(bSuccess, sExtraData)
   {
      var _loc4_ = Number(sExtraData.charAt(0));
      var _loc5_ = _loc4_ != 1 ? this.api.datacenter.Exchange.payIfSuccessGarbage : this.api.datacenter.Exchange.payGarbage;
      var _loc6_ = _loc4_ != 1 ? "payIfSuccessKama" : "payKama";
      switch(this.api.datacenter.Basics.aks_exchange_echangeType)
      {
         case 12:
            this.modifyDistant(sExtraData.substr(2),_loc5_,_loc6_,false);
            return undefined;
         case 13:
            this.modifyLocal(sExtraData.substr(2),_loc5_,_loc6_);
      }
      return undefined;
   }
   function modifyLocal(sExtraData, ea, sKamaLocation, sOgrineLocation)
   {
      var _loc6_ = sExtraData.charAt(0);
      var _loc7_ = this.api.datacenter.Exchange;
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
      switch(_loc6_)
      {
         case "O":
            _loc8_ = sExtraData.charAt(1) == "+";
            _loc9_ = sExtraData.substr(2).split("|");
            _loc10_ = Number(_loc9_[0]);
            _loc11_ = Number(_loc9_[1]);
            _loc12_ = this.api.datacenter.Player.Inventory.findFirstItem("ID",_loc10_);
            _loc13_ = _loc7_.inventory.findFirstItem("ID",_loc10_);
            _loc14_ = ea.findFirstItem("ID",_loc10_);
            if(_loc8_)
            {
               _loc15_ = _loc13_.item;
               _loc16_ = new dofus.datacenter.Item(_loc10_,_loc15_.unicID,_loc11_,-2,_loc15_.compressedEffects);
               _loc17_ = -1;
               _loc18_ = _loc12_.item.Quantity - _loc11_;
               if(_loc18_ == 0)
               {
                  _loc17_ = -3;
               }
               _loc13_.item.Quantity = _loc18_;
               _loc13_.item.position = _loc17_;
               _loc7_.inventory.updateItem(_loc13_.index,_loc13_.item);
               if(_loc14_.index != -1)
               {
                  ea.updateItem(_loc14_.index,_loc16_);
               }
               else
               {
                  ea.push(_loc16_);
               }
            }
            else if(_loc14_.index != -1)
            {
               _loc13_.item.position = -1;
               _loc13_.item.Quantity = _loc12_.item.Quantity;
               _loc7_.inventory.updateItem(_loc13_.index,_loc13_.item);
               ea.removeItems(_loc14_.index,1);
            }
            return;
         case "G":
            _loc19_ = Number(sExtraData.substr(1));
            _loc7_[sKamaLocation] = _loc19_;
            return;
         case "S":
            _loc19_ = Number(sExtraData.substr(1));
            _loc7_[sOgrineLocation] = _loc19_;
      }
      return undefined;
   }
   function modifyLocal2(sExtraData, ea, sKamaLocation, sOgrineLocation)
   {
      var _loc6_ = sExtraData.charAt(0);
      var _loc7_ = this.api.datacenter.Exchange;
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
      switch(_loc6_)
      {
         case "O":
            _loc8_ = sExtraData.charAt(1) == "+";
            _loc9_ = sExtraData.substr(2).split("|");
            _loc10_ = Number(_loc9_[0]);
            _loc11_ = Number(_loc9_[1]);
            _loc12_ = this.api.datacenter.Player.Inventory.findFirstItem("ID",_loc10_);
            _loc13_ = _loc7_.inventory.findFirstItem("ID",_loc10_);
            _loc14_ = ea.findFirstItem("ID",_loc10_);
            if(_loc8_)
            {
               _loc15_ = _loc13_.item;
               _loc16_ = new dofus.datacenter.Item(_loc10_,_loc15_.unicID,_loc11_,-2,_loc15_.compressedEffects);
               _loc17_ = -1;
               _loc18_ = _loc12_.item.Quantity - _loc11_;
               if(_loc18_ == 0)
               {
                  _loc17_ = -3;
               }
               _loc13_.item.Quantity = _loc18_;
               _loc13_.item.position = _loc17_;
               _loc7_.inventory.startNoEventDispatchsPeriod(dofus.Constants.DELAYED_INVENTORY_ITEMS_VISUAL_REFRESH);
               _loc7_.inventory.updateItem(_loc13_.index,_loc13_.item);
               if(_loc14_.index != -1)
               {
                  ea.startNoEventDispatchsPeriod(dofus.Constants.DELAYED_INVENTORY_ITEMS_VISUAL_REFRESH);
                  ea.updateItem(_loc14_.index,_loc16_);
                  return undefined;
               }
               ea.startNoEventDispatchsPeriod(dofus.Constants.DELAYED_INVENTORY_ITEMS_VISUAL_REFRESH);
               ea.push(_loc16_);
               return undefined;
            }
            if(_loc14_.index != -1)
            {
               _loc13_.item.position = -1;
               _loc13_.item.Quantity = _loc12_.item.Quantity;
               _loc7_.inventory.startNoEventDispatchsPeriod(dofus.Constants.DELAYED_INVENTORY_ITEMS_VISUAL_REFRESH);
               _loc7_.inventory.updateItem(_loc13_.index,_loc13_.item);
               ea.startNoEventDispatchsPeriod(dofus.Constants.DELAYED_INVENTORY_ITEMS_VISUAL_REFRESH);
               ea.removeItems(_loc14_.index,1);
               return undefined;
            }
            return undefined;
            break;
         case "G":
            _loc19_ = Number(sExtraData.substr(1));
            _loc7_[sKamaLocation] = _loc19_;
            break;
         case "S":
            _loc19_ = Number(sExtraData.substr(1));
            _loc7_[sOgrineLocation] = _loc19_;
      }
      return undefined;
   }
   function modifyDistant(sExtraData, ea, sKamaLocation, bForceModifyInventory, sOgrineLocation)
   {
      var _loc8_ = sExtraData.charAt(0);
      var _loc9_ = this.api.datacenter.Exchange;
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
      switch(_loc8_)
      {
         case "O":
            _loc10_ = sExtraData.charAt(1) == "+";
            _loc11_ = sExtraData.substr(2).split("|");
            _loc12_ = Number(_loc11_[0]);
            _loc13_ = Number(_loc11_[1]);
            _loc14_ = Number(_loc11_[2]);
            _loc15_ = _loc11_[3];
            _loc16_ = ea.findFirstItem("ID",_loc12_);
            if(_loc10_)
            {
               _loc17_ = new dofus.datacenter.Item(_loc12_,_loc14_,_loc13_,-1,_loc15_);
               _loc18_ = bForceModifyInventory == undefined ? _loc9_.distantPlayerID == undefined : bForceModifyInventory;
               if(_loc16_.index != -1)
               {
                  ea.startNoEventDispatchsPeriod(dofus.Constants.DELAYED_INVENTORY_ITEMS_VISUAL_REFRESH);
                  ea.updateItem(_loc16_.index,_loc17_);
               }
               else
               {
                  ea.startNoEventDispatchsPeriod(dofus.Constants.DELAYED_INVENTORY_ITEMS_VISUAL_REFRESH);
                  ea.push(_loc17_);
               }
               if(_loc18_)
               {
                  _loc19_ = _loc9_.inventory.findFirstItem("ID",_loc12_);
                  if(_loc19_.index != -1)
                  {
                     _loc19_.item.position = -1;
                     _loc19_.item.Quantity = Number(_loc19_.item.Quantity) + Number(_loc13_);
                     _loc9_.inventory.startNoEventDispatchsPeriod(dofus.Constants.DELAYED_INVENTORY_ITEMS_VISUAL_REFRESH);
                     _loc9_.inventory.updateItem(_loc19_.index,_loc19_.item);
                     return undefined;
                  }
                  _loc9_.inventory.startNoEventDispatchsPeriod(dofus.Constants.DELAYED_INVENTORY_ITEMS_VISUAL_REFRESH);
                  _loc9_.inventory.push(_loc17_);
                  _global.API.ui.getUIComponent("Craft").updateForgemagusResult(_loc17_);
                  return undefined;
               }
               return undefined;
            }
            if(_loc16_.index != -1)
            {
               ea.startNoEventDispatchsPeriod(dofus.Constants.DELAYED_INVENTORY_ITEMS_VISUAL_REFRESH);
               ea.removeItems(_loc16_.index,1);
               return undefined;
            }
            return undefined;
            break;
         case "G":
            _loc20_ = Number(sExtraData.substr(1));
            _loc9_[sKamaLocation] = _loc20_;
            break;
         case "S":
            _loc20_ = Number(sExtraData.substr(1));
            _loc9_[sOgrineLocation] = _loc20_;
      }
      return undefined;
   }
   function onStorageMovement(bSuccess, sExtraData)
   {
      var _loc4_ = sExtraData.charAt(0);
      var _loc5_ = this.api.datacenter.Temporary.Storage;
      var _loc6_ = _loc5_.inventory;
      var _loc7_;
      var _loc8_;
      var _loc9_;
      var _loc10_;
      var _loc11_;
      var _loc12_;
      var _loc13_;
      var _loc14_;
      var _loc15_;
      switch(_loc4_)
      {
         case "O":
            _loc7_ = sExtraData.charAt(1) == "+";
            _loc8_ = sExtraData.substr(2).split("|");
            _loc9_ = Number(_loc8_[0]);
            _loc10_ = Number(_loc8_[1]);
            _loc11_ = Number(_loc8_[2]);
            _loc12_ = _loc8_[3];
            _loc13_ = _loc6_.findFirstItem("ID",_loc9_);
            if(_loc7_)
            {
               _loc14_ = new dofus.datacenter.Item(_loc9_,_loc11_,_loc10_,-1,_loc12_);
               if(_loc13_.index != -1)
               {
                  _loc6_.startNoEventDispatchsPeriod(dofus.Constants.DELAYED_INVENTORY_ITEMS_VISUAL_REFRESH);
                  _loc6_.updateItem(_loc13_.index,_loc14_);
                  return undefined;
               }
               _loc6_.startNoEventDispatchsPeriod(dofus.Constants.DELAYED_INVENTORY_ITEMS_VISUAL_REFRESH);
               _loc6_.push(_loc14_);
               return undefined;
            }
            if(_loc13_.index != -1)
            {
               _loc6_.startNoEventDispatchsPeriod(dofus.Constants.DELAYED_INVENTORY_ITEMS_VISUAL_REFRESH);
               _loc6_.removeItems(_loc13_.index,1);
               return undefined;
            }
            ank.utils.Logger.err("[onStorageMovement] cet objet n\'existe pas id=" + _loc9_);
            return undefined;
            break;
         case "G":
            _loc15_ = Number(sExtraData.substr(1));
            _loc5_.Kama = _loc15_;
      }
      return undefined;
   }
   function onPlayerShopMovement(bSuccess, sExtraData)
   {
      var _loc4_ = sExtraData.charAt(0) == "+";
      var _loc5_ = sExtraData.substr(1).split("|");
      var _loc6_ = Number(_loc5_[0]);
      var _loc7_ = Number(_loc5_[1]);
      var _loc8_ = Number(_loc5_[2]);
      var _loc9_ = _loc5_[3];
      var _loc10_ = Number(_loc5_[4]);
      var _loc11_ = this.api.datacenter.Temporary.Shop;
      var _loc12_ = _loc11_.inventory.findFirstItem("ID",_loc6_);
      var _loc13_;
      if(_loc4_)
      {
         _loc13_ = new dofus.datacenter.Item(_loc6_,_loc8_,_loc7_,-1,_loc9_,_loc10_);
         if(_loc12_.index != -1)
         {
            _loc11_.inventory.updateItem(_loc12_.index,_loc13_);
         }
         else
         {
            _loc11_.inventory.push(_loc13_);
         }
      }
      else if(_loc12_.index != -1)
      {
         _loc11_.inventory.removeItems(_loc12_.index,1);
      }
      else
      {
         ank.utils.Logger.err("[onPlayerShopMovement] cet objet n\'existe pas id=" + _loc6_);
      }
      this.api.ui.getUIComponent("PlayerShopModifier").refreshRemoveButton();
   }
   function onList(sExtraData)
   {
      var _loc4_;
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
      var _loc30_;
      var _loc31_;
      var _loc32_;
      var _loc33_;
      var _loc34_;
      var _loc35_;
      var _loc36_;
      var _loc37_;
      var _loc38_;
      var _loc39_;
      var _loc40_;
      var _loc41_;
      var _loc42_;
      var _loc43_;
      var _loc44_;
      var _loc45_;
      switch(this.api.datacenter.Basics.aks_exchange_echangeType)
      {
         case 0:
            _loc4_ = this.api.datacenter.Player.Inventory;
            _loc5_ = 0;
            while(_loc5_ < _loc4_.length)
            {
               _loc6_ = _loc4_[_loc5_];
               _loc6_.resellCustomPrice = undefined;
               _loc6_.customMoneyItemId = undefined;
               _loc5_ += 1;
            }
            _loc7_ = sExtraData.split("|");
            _loc8_ = new ank.utils.ExtendedArray();
            for(_loc45_ in _loc7_)
            {
               _loc9_ = _loc7_[_loc45_].split(";");
               _loc10_ = Number(_loc9_[0]);
               _loc11_ = _loc9_[1];
               _loc12_ = Number(_loc9_[2]);
               _loc13_ = Number(_loc9_[3]);
               if(_global.isNaN(_loc12_))
               {
                  _loc12_;
               }
               if(_global.isNaN(_loc13_))
               {
                  _loc13_;
               }
               _loc14_ = Number(_loc9_[4]);
               if(_global.isNaN(_loc14_))
               {
                  _loc14_;
               }
               else
               {
                  _loc15_ = 0;
                  while(_loc15_ < _loc4_.length)
                  {
                     _loc16_ = _loc4_[_loc15_];
                     if(_loc16_.unicID == _loc10_)
                     {
                        _loc16_.resellCustomPrice = _loc14_;
                        _loc16_.customMoneyItemId = _loc12_;
                     }
                     _loc15_ += 1;
                  }
               }
               _loc17_ = _loc9_[5] == "1";
               if(!_loc17_)
               {
                  _loc18_ = new dofus.datacenter.Item(0,_loc10_,undefined,undefined,_loc11_,_loc13_);
                  _loc18_.customMoneyItemId = _loc12_;
                  _loc18_.resellCustomPrice = _loc14_;
                  _loc18_.priceMultiplicator = this.api.lang.getConfigText("BUY_PRICE_MULTIPLICATOR");
                  _loc8_.push(_loc18_);
               }
            }
            this.api.datacenter.Temporary.Shop.inventory = _loc8_;
            return undefined;
         case 5:
         case 15:
         case 8:
            _loc19_ = sExtraData.split(";");
            _loc20_ = new ank.utils.ExtendedArray();
            for(_loc45_ in _loc19_)
            {
               _loc21_ = _loc19_[_loc45_];
               _loc22_ = _loc21_.charAt(0);
               _loc23_ = _loc21_.substr(1);
               switch(_loc22_)
               {
                  case "O":
                     _loc24_ = this.api.kernel.CharactersManager.getItemObjectFromData(_loc23_);
                     _loc20_.push(_loc24_);
                     break;
                  case "G":
                     this.onStorageKama(_loc23_);
               }
            }
            this.api.datacenter.Temporary.Storage.inventory = _loc20_;
            if(dofus.Constants.SAVING_THE_WORLD)
            {
               dofus.SaveTheWorld.getInstance().newItems(sExtraData);
               dofus.SaveTheWorld.getInstance().nextAction();
               return undefined;
            }
            return undefined;
            break;
         case 4:
         case 6:
            _loc25_ = sExtraData.split("|");
            _loc26_ = new ank.utils.ExtendedArray();
            for(_loc45_ in _loc25_)
            {
               _loc27_ = _loc25_[_loc45_].split(";");
               _loc28_ = Number(_loc27_[0]);
               _loc29_ = Number(_loc27_[1]);
               _loc30_ = Number(_loc27_[2]);
               _loc31_ = _loc27_[3];
               _loc32_ = Number(_loc27_[4]);
               _loc33_ = Number(_loc27_[5]);
               _loc34_ = new dofus.datacenter.Item(_loc28_,_loc30_,_loc29_,-1,_loc31_,_loc32_);
               if(!_global.isNaN(_loc33_))
               {
                  _loc34_.averagePrice = _loc33_;
               }
               _loc26_.push(_loc34_);
            }
            this.api.datacenter.Temporary.Shop.inventory = _loc26_;
            return undefined;
         case 10:
            _loc35_ = sExtraData.split("|");
            _loc36_ = new ank.utils.ExtendedArray();
            if(sExtraData.length != 0)
            {
               for(_loc45_ in _loc35_)
               {
                  _loc37_ = _loc35_[_loc45_].split(";");
                  _loc38_ = Number(_loc37_[0]);
                  _loc39_ = Number(_loc37_[1]);
                  _loc40_ = Number(_loc37_[2]);
                  _loc41_ = _loc37_[3];
                  _loc42_ = Number(_loc37_[4]);
                  _loc43_ = Number(_loc37_[5]);
                  _loc44_ = new dofus.datacenter.Item(_loc38_,_loc40_,_loc39_,-1,_loc41_,_loc42_);
                  _loc44_.remainingHours = _loc43_;
                  _loc36_.push(_loc44_);
               }
            }
            this.api.datacenter.Temporary.Shop.inventory = _loc36_;
      }
      return undefined;
   }
   function onSell(bSuccess)
   {
      if(bSuccess)
      {
         this.api.kernel.showMessage(undefined,this.api.lang.getText("SELL_DONE"),"INFO_CHAT");
      }
      else
      {
         this.api.kernel.showMessage(this.api.lang.getText("EXCHANGE"),this.api.lang.getText("CANT_SELL"),"ERROR_BOX",{name:"Sell"});
      }
   }
   function onBuy(bSuccess)
   {
      if(bSuccess)
      {
         this.api.kernel.showMessage(undefined,this.api.lang.getText("BUY_DONE"),"INFO_CHAT");
      }
      else
      {
         this.api.kernel.showMessage(this.api.lang.getText("EXCHANGE"),this.api.lang.getText("CANT_BUY"),"ERROR_BOX",{name:"Buy"});
      }
   }
   function onStorageKama(sExtraData)
   {
      var _loc3_ = Number(sExtraData);
      this.api.datacenter.Temporary.Storage.Kama = _loc3_;
   }
   function onBigStoreTypeItemsList(sExtraData)
   {
      var _loc3_ = sExtraData.split("|");
      var _loc4_ = Number(_loc3_[0]);
      var _loc5_ = _loc3_[1].split(";");
      var _loc6_ = new ank.utils.ExtendedArray();
      var _loc7_;
      var _loc8_;
      var _loc9_;
      if(_loc3_[1].length != 0)
      {
         _loc7_ = 0;
         while(_loc7_ < _loc5_.length)
         {
            _loc8_ = Number(_loc5_[_loc7_]);
            if(dofus.datacenter.Item.isFullSoul(_loc4_))
            {
               _loc9_ = new dofus.datacenter.MonsterInBidHouse(_loc8_,_loc4_);
            }
            else
            {
               _loc9_ = new dofus.datacenter.Item(0,_loc8_,1,-1,"",0);
            }
            _loc6_.push(_loc9_);
            _loc7_ += 1;
         }
      }
      this.api.datacenter.Temporary.Shop.inventory = _loc6_;
      this.api.ui.getUIComponent("BigStoreBuy").setType(_loc4_);
   }
   function onItemMiddlePriceInBigStore(sExtraData)
   {
      var _loc3_ = sExtraData.split("|");
      var _loc4_ = Number(_loc3_[0]);
      var _loc5_ = Number(_loc3_[1]);
      this.api.ui.getUIComponent("BigStoreBuy").setMiddlePrice(_loc4_,_loc5_);
      this.api.ui.getUIComponent("BigStoreSell").setMiddlePrice(_loc4_,_loc5_);
   }
   function onBigStoreTypeItemsMovement(sExtraData)
   {
      var _loc3_ = sExtraData.charAt(0) == "+";
      var _loc4_ = Number(sExtraData.substr(1));
      var _loc5_ = this.api.datacenter.Temporary.Shop;
      var _loc6_ = _loc5_.inventory.findFirstItem("unicID",_loc4_);
      var _loc7_;
      if(_loc3_)
      {
         _loc7_ = new dofus.datacenter.Item(0,_loc4_,0,-1,"",0);
         if(_loc6_.index != -1)
         {
            _loc5_.inventory.updateItem(_loc6_.index,_loc7_);
         }
         else
         {
            _loc5_.inventory.push(_loc7_);
         }
      }
      else if(_loc6_.index != -1)
      {
         _loc5_.inventory.removeItems(_loc6_.index,1);
      }
      else
      {
         ank.utils.Logger.err("[onBigStoreTypeItemsMovement] cet objet n\'existe pas unicID=" + _loc4_);
      }
   }
   function onBigStoreItemsList(sExtraData)
   {
      var _loc3_ = sExtraData.split("|");
      var _loc4_ = Number(_loc3_[0]);
      this.api.ui.getUIComponent("BigStoreBuy").setItem(_loc4_);
      var _loc5_ = new ank.utils.ExtendedArray();
      if(_loc3_[1].length == 0)
      {
         return this.api.datacenter.Temporary.Shop.inventory2 = _loc5_;
      }
      var _loc6_ = 1;
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
      var _loc20_;
      var _loc21_;
      while(_loc6_ < _loc3_.length)
      {
         _loc7_ = _loc3_[_loc6_].split(";");
         _loc8_ = Number(_loc7_[0]);
         _loc9_ = _loc7_[1];
         _loc10_ = _loc7_[2].split(",");
         _loc11_ = Number(_loc10_[0]);
         _loc12_ = !!Number(_loc10_[1]);
         _loc13_ = _loc7_[3].split(",");
         _loc14_ = Number(_loc13_[0]);
         _loc15_ = !!Number(_loc13_[1]);
         _loc16_ = _loc7_[4].split(",");
         _loc17_ = Number(_loc16_[0]);
         _loc18_ = !!Number(_loc16_[1]);
         _loc19_ = Number(_loc7_[5]);
         _loc20_ = new dofus.datacenter.Item(_loc8_,_loc19_,0,-1,_loc9_,0);
         _loc21_ = {id:_loc8_,item:_loc20_,priceSet1:_loc11_,priceSet2:_loc14_,priceSet3:_loc17_,isMySale1:_loc12_,isMySale2:_loc15_,isMySale3:_loc18_};
         _loc5_.push(_loc21_);
         _loc6_ += 1;
      }
      this.api.datacenter.Temporary.Shop.inventory2 = _loc5_;
   }
   function onBigStoreItemsMovement(sExtraData)
   {
      var _loc3_ = sExtraData.charAt(0) == "+";
      var _loc4_ = sExtraData.substr(1).split("|");
      var _loc5_ = Number(_loc4_[0]);
      var _loc6_ = Number(_loc4_[1]);
      var _loc7_ = _loc4_[2];
      var _loc8_ = _loc4_[3].split(",");
      var _loc9_ = Number(_loc8_[0]);
      var _loc10_ = !!Number(_loc8_[1]);
      var _loc11_ = _loc4_[4].split(",");
      var _loc12_ = Number(_loc11_[0]);
      var _loc13_ = !!Number(_loc11_[1]);
      var _loc14_ = _loc4_[5].split(",");
      var _loc15_ = Number(_loc14_[0]);
      var _loc16_ = !!Number(_loc14_[1]);
      var _loc17_ = this.api.datacenter.Temporary.Shop;
      var _loc18_ = _loc17_.inventory2.findFirstItem("id",_loc5_);
      var _loc19_;
      var _loc20_;
      if(_loc3_)
      {
         _loc19_ = new dofus.datacenter.Item(_loc5_,_loc6_,0,-1,_loc7_,0);
         _loc20_ = {id:_loc5_,item:_loc19_,priceSet1:_loc9_,priceSet2:_loc12_,priceSet3:_loc15_,isMySale1:_loc10_,isMySale2:_loc13_,isMySale3:_loc16_};
         if(_loc18_.index != -1)
         {
            _loc17_.inventory2.updateItem(_loc18_.index,_loc20_);
         }
         else
         {
            _loc17_.inventory2.push(_loc20_);
         }
         _loc17_.refreshInventory("modelChanged2");
         return undefined;
      }
      if(_loc18_.index != -1)
      {
         _loc17_.inventory2.removeItems(_loc18_.index,1);
         _loc17_.refreshInventory("modelChanged2");
      }
      else
      {
         ank.utils.Logger.err("[onBigStoreItemsMovement] cet objet n\'existe pas id=" + _loc5_);
      }
   }
   function onSearch(sExtraData)
   {
      this.api.ui.getUIComponent("BigStoreBuy").onSearchResult(sExtraData == "K");
   }
   function onCraftPublicMode(sExtraData)
   {
      var _loc3_;
      var _loc4_;
      var _loc5_;
      var _loc6_;
      var _loc7_;
      var _loc8_;
      if(sExtraData.length == 1)
      {
         _loc3_ = sExtraData;
         this.api.datacenter.Player.craftPublicMode = _loc3_ != "+" ? false : true;
      }
      else
      {
         _loc4_ = sExtraData.charAt(0);
         _loc5_ = sExtraData.substr(1).split("|");
         _loc6_ = _loc5_[0];
         _loc7_ = this.api.datacenter.Sprites.getItemAt(_loc6_);
         if(_loc4_ == "+" && _loc5_[1].length > 0)
         {
            _loc8_ = _loc5_[1].split(";");
            _loc7_.multiCraftSkillsID = _loc8_;
         }
         else
         {
            _loc7_.multiCraftSkillsID = undefined;
         }
      }
   }
   function onMountPods(sExtraData)
   {
      var _loc3_ = sExtraData.split(";");
      var _loc4_ = Number(_loc3_[0]);
      var _loc5_ = Number(_loc3_[1]);
      this.api.datacenter.Player.mount.podsMax = _loc5_;
      this.api.datacenter.Player.mount.pods = _loc4_;
   }
   function cancel(oEvent)
   {
      this.leave();
   }
   function yes(oEvent)
   {
      this.accept();
   }
   function no(oEvent)
   {
      this.leave();
   }
   function ignore(oEvent)
   {
      this.api.kernel.ChatManager.addToBlacklist(oEvent.params.player);
      this.api.kernel.showMessage(undefined,this.api.lang.getText("TEMPORARY_BLACKLISTED",[oEvent.params.player]),"INFO_CHAT");
      this.leave();
   }
   function onFmQuickAction(sExtraData)
   {
      if(sExtraData == undefined || sExtraData.length < 2)
      {
         return undefined;
      }
      var _loc3_ = sExtraData.charAt(0);
      var _loc4_ = sExtraData.substr(1);
      switch(_loc3_)
      {
         case "F":
            this.handleFmFuseResponse(_loc4_);
            return undefined;
         case "B":
            this.handleFmBulkResponse(_loc4_);
            return undefined;
         default:
            ank.utils.Logger.err("[FM v2] Unknown FM packet type: " + _loc3_);
            return undefined;
      }
   }
   function handleFmFuseResponse(sData)
   {
      var _loc3_;
      var _loc4_;
      if(sData.charAt(0) == "E")
      {
         _loc3_ = sData.charAt(1);
         _loc4_ = "";
         switch(_loc3_)
         {
            case "0":
               _loc4_ = "Format de packet invalide";
               break;
            case "1":
               _loc4_ = "Placez d\'abord un objet dans le slot FM";
               break;
            case "2":
               _loc4_ = "Vous ne possédez pas cette rune";
               break;
            case "3":
               _loc4_ = "Cette rune est incompatible";
               break;
            case "4":
               _loc4_ = "Une opération est déjà en cours";
               break;
            case "5":
               _loc4_ = "Trop de requêtes, attendez un instant";
               break;
            default:
               _loc4_ = "Erreur inconnue";
         }
         this.api.kernel.showMessage(undefined,"[FM] " + _loc4_,"ERROR_CHAT");
      }
   }
   function handleFmBulkResponse(sData)
   {
      var _loc3_;
      var _loc4_;
      var _loc5_;
      if(sData.substr(0,2) == "OK")
      {
         _loc3_ = Number(sData.substr(2));
         this.api.kernel.showMessage(undefined,"[FM] " + _loc3_ + " runes transférées","INFO_CHAT");
      }
      else if(sData.charAt(0) == "E")
      {
         _loc4_ = sData.charAt(1);
         _loc5_ = "";
         switch(_loc4_)
         {
            case "f":
               _loc5_ = "Format de packet invalide";
               break;
            case "i":
               _loc5_ = "Ce n\'est pas un type de rune valide";
               break;
            case "n":
               _loc5_ = "Aucune rune de ce type dans votre inventaire";
               break;
            default:
               _loc5_ = "Erreur inconnue";
         }
         this.api.kernel.showMessage(undefined,"[FM] " + _loc5_,"ERROR_CHAT");
      }
   }
}
