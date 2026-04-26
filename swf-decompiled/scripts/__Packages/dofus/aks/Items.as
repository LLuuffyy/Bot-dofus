class dofus.aks.Items extends dofus.aks.Handler
{
   var api;
   static var EFFECT_APPEND_CHAR = ":";
   static var COMPRESSION_RADIX = 16;
   static var MAX_BATCH_ITEM_USE = 300;
   function Items(oAKS, oAPI)
   {
      super.initialize(oAKS,oAPI);
   }
   function movement(nID, nPosition, nQuantity)
   {
      if(nPosition > 0)
      {
         this.api.kernel.GameManager.setAsModified(nPosition);
      }
      this.aks.send("OM" + nID + "|" + nPosition + (!_global.isNaN(nQuantity) ? "|" + nQuantity : ""),true);
   }
   function drop(nID, nQuantity)
   {
      this.aks.send("OD" + nID + "|" + nQuantity,false);
   }
   function associateMimibiote(nIDToAttach, nIDToEat)
   {
      this.aks.send("AEi1|" + nIDToAttach + "|" + nIDToEat);
   }
   function destroyMimibiote(nID)
   {
      this.aks.send("AEi0|" + nID);
   }
   function selectRouletteItem(nIndex)
   {
      this.aks.send("wc" + nIndex,false);
   }
   function destroy(nID, nQuantity)
   {
      this.aks.send("Od" + nID + "|" + nQuantity,false);
   }
   function reinitialize(nID)
   {
      this.aks.send("OR" + nID,false);
   }
   function use(nID, sSpriteID, nCellNum, bConfirm, nQuantity)
   {
      if(nQuantity == undefined)
      {
         nQuantity = 1;
      }
      this.aks.send("O" + (!bConfirm ? "U" : "u") + nID + (!(sSpriteID != undefined && !_global.isNaN(Number(sSpriteID))) ? "|" : "|" + sSpriteID) + (nCellNum == undefined ? "|" : "|" + nCellNum) + "|" + nQuantity,true);
   }
   function dissociate(nID, nPosition)
   {
      this.aks.send("Ox" + nID + "|" + nPosition,false);
   }
   function setSkin(nID, nPosition, nSkin)
   {
      this.aks.send("Os" + nID + "|" + nPosition + "|" + nSkin,false);
   }
   function feed(nID, nPosition, nFeededItemId)
   {
      this.aks.send("Of" + nID + "|" + nPosition + "|" + nFeededItemId,false);
   }
   function applyFragment(nFragmentID, nTargetID)
   {
      this.aks.send("OF" + nFragmentID + "|" + nTargetID,true);
   }
   function applySpellRune(nRuneID, nTargetID)
   {
      this.aks.send("OZ" + nRuneID + "|" + nTargetID,true);
   }
   function lock(nID, bLock, nDays)
   {
      this.aks.send("Ol" + nID + "," + bLock + "," + (nDays != undefined ? nDays : ""),false);
   }
   function playerLock(nID, bLock)
   {
      this.aks.send("OL" + nID + "|" + (bLock ? "1" : "0"),false);
   }
   function onPlayerLock(sData)
   {
      var _loc3_ = sData.split("|");
      var _loc4_ = Number(_loc3_[0]);
      var _loc5_ = _loc3_[1] == "1";
      var _loc6_ = this.api.datacenter.Player.Inventory;
      var _loc7_ = 0;
      while(_loc7_ < _loc6_.length)
      {
         if(_loc6_[_loc7_].ID == _loc4_)
         {
            _loc6_[_loc7_].isPlayerLocked = _loc5_;
            _loc6_.dispatchEvent({type:"modelChanged",eventName:"updateAll"});
            break;
         }
         _loc7_ += 1;
      }
   }
   function equipItem(oItem)
   {
      if(oItem.isEquiped || oItem.isShortcut)
      {
         return false;
      }
      var _loc4_ = oItem.superType;
      var _loc5_;
      if(oItem.superType != 8)
      {
         _loc5_ = 0;
         while(_loc5_ < dofus.graphics.gapi.ui.Inventory.SUPERTYPE_NOT_EQUIPABLE.length)
         {
            if(dofus.graphics.gapi.ui.Inventory.SUPERTYPE_NOT_EQUIPABLE[_loc5_] == _loc4_)
            {
               return false;
            }
            _loc5_ += 1;
         }
      }
      var _loc6_ = this.api.lang.getSlotsFromSuperType(oItem.superType);
      if(_loc4_ == 13)
      {
         _loc6_ = _loc6_.concat([26,27,28,29,30,31]);
      }
      var _loc7_;
      var _loc8_ = 0;
      var _loc9_;
      var _loc10_;
      while(_loc8_ < _loc6_.length)
      {
         _loc9_ = Number(_loc6_[_loc8_]);
         _loc10_ = this.api.datacenter.Player.InventoryByItemPositions.getItemAt(_loc9_) == undefined;
         if(_loc10_)
         {
            _loc7_ = _loc9_;
            break;
         }
         _loc8_ += 1;
      }
      var _loc11_ = _loc7_ == undefined;
      var _loc12_;
      var _loc13_;
      if(_loc11_)
      {
         _loc12_ = getTimer();
         _loc13_ = 0;
         while(_loc13_ < _loc6_.length)
         {
            if(this.api.kernel.GameManager.getLastModified(_loc6_[_loc13_]) < _loc12_)
            {
               _loc12_ = this.api.kernel.GameManager.getLastModified(_loc6_[_loc13_]);
               _loc7_ = _loc6_[_loc13_];
            }
            _loc13_ += 1;
         }
      }
      if(_loc7_ == undefined || _global.isNaN(_loc7_))
      {
         return false;
      }
      this.api.network.Items.movement(oItem.ID,_loc7_);
      return true;
   }
   function onAccessories(sExtraData)
   {
      var _loc4_ = sExtraData.split("|");
      var _loc5_ = _loc4_[0];
      var _loc6_ = _loc4_[1].split(",");
      var _loc7_ = [];
      var _loc8_ = 0;
      var _loc9_;
      var _loc10_;
      var _loc11_;
      var _loc12_;
      var _loc13_;
      while(_loc8_ < _loc6_.length)
      {
         if(_loc6_[_loc8_].indexOf("~") != -1)
         {
            _loc9_ = _loc6_[_loc8_].split("~");
            _loc10_ = _global.parseInt(_loc9_[0],16);
            _loc11_ = _global.parseInt(_loc9_[1]);
            _loc12_ = _global.parseInt(_loc9_[2]) - 1;
            if(_loc12_ < 0)
            {
               _loc12_ = 0;
            }
         }
         else
         {
            _loc10_ = _global.parseInt(_loc6_[_loc8_],16);
            _loc11_;
            _loc12_;
         }
         if(!_global.isNaN(_loc10_))
         {
            _loc13_ = new dofus.datacenter.Accessory(_loc10_,_loc11_,_loc12_);
            _loc7_[_loc8_] = _loc13_;
         }
         _loc8_ += 1;
      }
      var _loc14_ = this.api.datacenter.Sprites.getItemAt(_loc5_);
      _loc14_.accessories = _loc7_;
      this.api.gfx.setForcedSpriteAnim(_loc5_,"static");
      if(_loc5_ == this.api.datacenter.Player.ID)
      {
         this.api.datacenter.Player.updateCloseCombat();
      }
   }
   function onDrop(bSuccess, sExtraData)
   {
      if(!bSuccess)
      {
         if(sExtraData == "F")
         {
            this.api.kernel.showMessage(undefined,this.api.lang.getText("DROP_FULL"),"ERROR_BOX",{name:"DropFull"});
         }
         else if(sExtraData == "E")
         {
            this.api.kernel.showMessage(undefined,this.api.lang.getText("CANT_DROP_ITEM"),"ERROR_BOX");
         }
      }
   }
   function onAdd(bSuccess, sExtraData)
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
      if(!bSuccess)
      {
         if(sExtraData == "F")
         {
            this.api.kernel.showMessage(undefined,this.api.lang.getText("INVENTORY_FULL"),"ERROR_BOX",{name:"Full"});
         }
         else if(sExtraData == "L")
         {
            this.api.kernel.showMessage(undefined,this.api.lang.getText("TOO_LOW_LEVEL_FOR_ITEM"),"ERROR_BOX",{name:"LowLevel"});
         }
         else if(sExtraData == "A")
         {
            this.api.kernel.showMessage(undefined,this.api.lang.getText("ALREADY_EQUIPED"),"ERROR_BOX",{name:"Already"});
         }
         return undefined;
      }
      _loc4_ = sExtraData.split("*");
      _loc5_ = 0;
      while(_loc5_ < _loc4_.length)
      {
         _loc6_ = _loc4_[_loc5_];
         _loc7_ = _loc6_.charAt(0);
         _loc6_ = _loc6_.substr(1);
         if(_loc7_ == "O")
         {
            _loc8_ = _loc6_.split(";");
            _loc9_ = 0;
            while(_loc9_ < _loc8_.length)
            {
               _loc10_ = this.api.kernel.CharactersManager.getItemObjectFromData(_loc8_[_loc9_]);
               if(this.api.datacenter.Basics.aks_exchange_echangeType == 0)
               {
                  _loc11_ = this.api.datacenter.Temporary.Shop.inventory;
                  _loc12_ = 0;
                  while(_loc12_ < _loc11_.length)
                  {
                     _loc13_ = _loc11_[_loc12_];
                     if(_loc13_.hasCustomResellCustomPrice)
                     {
                        if(_loc10_.unicID == _loc13_.unicID)
                        {
                           _loc10_.resellCustomPrice = _loc13_.resellCustomPrice;
                           _loc10_.customMoneyItemId = _loc13_.customMoneyItemId;
                        }
                     }
                     _loc12_ += 1;
                  }
               }
               if(_loc10_ != undefined)
               {
                  this.api.datacenter.Player.addItem(_loc10_);
               }
               _loc9_ += 1;
            }
         }
         else if(_loc7_ != "G")
         {
            ank.utils.Logger.err("Ajout d\'un type obj inconnu");
         }
         _loc5_ += 1;
      }
   }
   function onChange(sExtraData)
   {
      var _loc3_ = sExtraData.split("*");
      var _loc4_ = 0;
      var _loc5_;
      var _loc6_;
      var _loc7_;
      var _loc8_;
      while(_loc4_ < _loc3_.length)
      {
         _loc5_ = _loc3_[_loc4_];
         _loc6_ = _loc5_.split(";");
         _loc7_ = 0;
         while(_loc7_ < _loc6_.length)
         {
            _loc8_ = this.api.kernel.CharactersManager.getItemObjectFromData(_loc6_[_loc7_]);
            if(_loc8_ != undefined)
            {
               this.api.datacenter.Player.updateItem(_loc8_);
            }
            _loc7_ += 1;
         }
         _loc4_ += 1;
      }
   }
   function onRemove(sExtraData)
   {
      var _loc3_ = Number(sExtraData);
      this.api.datacenter.Player.dropItem(_loc3_);
   }
   function onQuantity(sExtraData)
   {
      var _loc3_ = sExtraData.split("|");
      var _loc4_ = Number(_loc3_[0]);
      var _loc5_ = Number(_loc3_[1]);
      this.api.datacenter.Player.updateItemQuantity(_loc4_,_loc5_);
   }
   function onMovement(sExtraData)
   {
      var _loc4_ = sExtraData.split("|");
      var _loc5_ = Number(_loc4_[0]);
      var _loc6_ = !_global.isNaN(Number(_loc4_[1])) ? Number(_loc4_[1]) : -1;
      this.api.datacenter.Player.updateItemPosition(_loc5_,_loc6_);
   }
   function onTool(sExtraData)
   {
      var _loc4_ = Number(sExtraData);
      if(_global.isNaN(_loc4_))
      {
         this.api.datacenter.Player.currentJobID = undefined;
      }
      else
      {
         this.api.datacenter.Player.currentJobID = _loc4_;
      }
   }
   function onDeletion(sExtraData)
   {
      var _loc3_ = sExtraData.charAt(0);
      if(_loc3_ == "E")
      {
         this.api.kernel.showMessage(undefined,this.api.lang.getText("CANNOT_DELETE_THIS_OBJECT"),"ERROR_CHAT");
      }
   }
   function onWeight(sExtraData)
   {
      var _loc3_ = sExtraData.split("|");
      var _loc4_ = Number(_loc3_[0]);
      var _loc5_ = Number(_loc3_[1]);
      var _loc6_ = Number(_loc3_[2]);
      this.api.datacenter.Player.maxWeight = _loc5_;
      this.api.datacenter.Player.currentWeight = _loc4_;
      this.api.datacenter.Player.maxOverWeight = _loc6_;
   }
   function onItemSet(sExtraData)
   {
      var _loc3_ = sExtraData.charAt(0) == "+";
      var _loc4_ = sExtraData.substr(1).split("|");
      var _loc5_ = Number(_loc4_[0]);
      var _loc6_ = String(_loc4_[1]).split(";");
      var _loc7_ = _loc4_[2];
      var _loc8_;
      if(_loc3_)
      {
         _loc8_ = new dofus.datacenter.ItemSet(_loc5_,_loc7_,_loc6_);
         this.api.datacenter.Player.ItemSets.addItemAt(_loc5_,_loc8_);
      }
      else
      {
         this.api.datacenter.Player.ItemSets.removeItemAt(_loc5_);
      }
   }
   function onItemUseCondition(sExtraData)
   {
      var _loc4_ = sExtraData.charAt(0);
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
      if(_loc4_ == "G")
      {
         _loc5_ = sExtraData.substr(1).split("|");
         _loc6_ = !_global.isNaN(Number(_loc5_[0])) ? Number(_loc5_[0]) : 0;
         _loc7_ = !_global.isNaN(Number(_loc5_[1])) ? Number(_loc5_[1]) : undefined;
         _loc8_ = !_global.isNaN(Number(_loc5_[2])) ? Number(_loc5_[2]) : undefined;
         _loc9_ = !_global.isNaN(Number(_loc5_[3])) ? Number(_loc5_[3]) : undefined;
         _loc10_ = {name:"UseItemGold",listener:this,params:{objectID:_loc6_,spriteID:_loc7_,cellID:_loc8_}};
         this.api.kernel.showMessage(undefined,this.api.lang.getText("ITEM_USE_CONDITION_GOLD",[_loc9_]),"CAUTION_YESNO",_loc10_);
      }
      else if(_loc4_ == "U")
      {
         _loc11_ = sExtraData.substr(1).split("|");
         _loc12_ = !_global.isNaN(Number(_loc11_[0])) ? Number(_loc11_[0]) : 0;
         _loc13_ = !_global.isNaN(Number(_loc11_[1])) ? Number(_loc11_[1]) : undefined;
         _loc14_ = !_global.isNaN(Number(_loc11_[2])) ? Number(_loc11_[2]) : undefined;
         _loc15_ = !_global.isNaN(Number(_loc11_[3])) ? Number(_loc11_[3]) : undefined;
         _loc16_ = {name:"UseItem",listener:this,params:{objectID:_loc12_,spriteID:_loc13_,cellID:_loc14_}};
         _loc17_ = new dofus.datacenter.Item(-1,_loc15_,1,0,"",0);
         this.api.kernel.showMessage(undefined,this.api.lang.getText("ITEM_USE_CONFIRMATION",[_loc17_.name]),"CAUTION_YESNO",_loc16_);
      }
   }
   function onItemFound(sExtraData)
   {
      var _loc4_ = sExtraData.split("|");
      var _loc5_ = !_global.isNaN(Number(_loc4_[0])) ? Number(_loc4_[0]) : 0;
      var _loc6_ = !_global.isNaN(Number(_loc4_[2])) ? Number(_loc4_[2]) : 0;
      var _loc7_ = _loc4_[1].split("~");
      var _loc8_ = !_global.isNaN(Number(_loc7_[0])) ? Number(_loc7_[0]) : 0;
      var _loc9_ = !_global.isNaN(Number(_loc7_[1])) ? Number(_loc7_[1]) : 0;
      var _loc10_;
      if(_loc5_ == 1)
      {
         if(_loc8_ == 0)
         {
            _loc10_ = {iconFile:"KamaSymbol"};
         }
         else
         {
            _loc10_ = new dofus.datacenter.Item(0,_loc8_,_loc9_);
         }
         this.api.gfx.addSpriteOverHeadItem(this.api.datacenter.Player.ID,"itemFound",dofus.graphics.battlefield.CraftResultOverHead,[true,_loc10_],2000);
      }
   }
   function yes(oEvent)
   {
      if(oEvent.target._name == "AskYesNoUseItemGold")
      {
         this.use(oEvent.params.objectID,oEvent.params.spriteID,oEvent.params.cellID,true);
      }
      else if(oEvent.target._name == "AskYesNoUseItem")
      {
         this.use(oEvent.params.objectID,oEvent.params.spriteID,oEvent.params.cellID,true);
      }
   }
}
