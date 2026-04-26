class dofus.managers.EffectsManager extends dofus.utils.ApiElement
{
   var _eaEffects;
   var _oSprite;
   var dispatchEvent;
   function EffectsManager(oSprite, oAPI)
   {
      super();
      var _loc5_ = new flash.display.BitmapData();
      this.initialize(oSprite,oAPI);
   }
   function initialize(oSprite, oAPI)
   {
      super.initialize(oAPI);
      this._oSprite = oSprite;
      this._eaEffects = new ank.utils.ExtendedArray();
      this._eaEffects.addEventListener("modelChanged",this);
      mx.events.EventDispatcher.initialize(this);
   }
   function getEffects()
   {
      return this._eaEffects;
   }
   function addEffect(oEffect)
   {
      var _loc3_ = 0;
      var _loc4_;
      while(_loc3_ < this._eaEffects.length)
      {
         _loc4_ = this._eaEffects[_loc3_];
         if(_loc4_.spellID == oEffect.spellID && (_loc4_.type == oEffect.type && _loc4_.remainingTurn == oEffect.remainingTurn))
         {
            _loc4_.param1 += oEffect.param1;
            return undefined;
         }
         _loc3_ += 1;
      }
      this._eaEffects.push(oEffect);
   }
   function terminateAllEffects(bForced)
   {
      var _loc3_ = this._eaEffects.length;
      var _loc4_;
      while((_loc3_ -= 1) >= 0)
      {
         _loc4_ = this._eaEffects[_loc3_];
         if(bForced || _loc4_.isDispellable)
         {
            this._eaEffects.removeItems(_loc3_,_loc3_ + 1);
         }
      }
   }
   function removeEffectsByCasterID(sCasterID)
   {
      if(sCasterID == undefined)
      {
         return undefined;
      }
      var _loc3_ = this._eaEffects.length;
      var _loc4_;
      while((_loc3_ -= 1) >= 0)
      {
         _loc4_ = this._eaEffects[_loc3_];
         if(_loc4_.sCasterID == sCasterID)
         {
            this._eaEffects.removeItems(_loc3_,1);
         }
      }
   }
   function removeEffectsByType(nType)
   {
      if(nType == undefined)
      {
         return undefined;
      }
      var _loc3_ = this._eaEffects.length;
      var _loc4_;
      while((_loc3_ -= 1) >= 0)
      {
         _loc4_ = this._eaEffects[_loc3_];
         if(_loc4_.type == nType)
         {
            this._eaEffects.removeItems(_loc3_,1);
         }
      }
   }
   function nextTurn()
   {
      var _loc2_ = this._eaEffects.length;
      var _loc3_;
      while((_loc2_ -= 1) >= 0)
      {
         _loc3_ = this._eaEffects[_loc2_];
         _loc3_.remainingTurn--;
         if(_loc3_.remainingTurn <= 0)
         {
            this._eaEffects.removeItems(_loc2_,1);
         }
      }
   }
   function modelChanged(oEvent)
   {
      this.dispatchEvent({type:"effectsChanged"});
   }
   static function getConditionalElementFromString(sElement, sConditions)
   {
      var _loc4_ = _global.API;
      var _loc5_ = [">","<","=","!"];
      if(sConditions == undefined || sConditions.length == 0)
      {
         return undefined;
      }
      var _loc6_ = sConditions.split("&");
      var _loc7_ = [];
      var _loc8_ = 0;
      var _loc9_;
      var _loc10_;
      var _loc11_;
      var _loc12_;
      var _loc13_;
      var _loc14_;
      var _loc15_;
      var _loc16_;
      while(_loc8_ < _loc6_.length)
      {
         _loc6_[_loc8_] = new ank.utils.ExtendedString(_loc6_[_loc8_]).replace(["(",")"],["",""]);
         _loc9_ = _loc6_[_loc8_].split("|");
         _loc10_ = 0;
         while(_loc10_ < _loc9_.length)
         {
            _loc11_ = 0;
            while(_loc11_ < _loc5_.length)
            {
               _loc12_ = _loc5_[_loc11_];
               _loc13_ = _loc9_[_loc10_].split(_loc12_);
               if(_loc13_.length > 1)
               {
                  break;
               }
               _loc11_ += 1;
            }
            if(_loc13_ != undefined)
            {
               _loc14_ = String(_loc13_[0]);
               _loc15_ = _loc13_[1];
               if((_loc16_ = _loc14_) === sElement)
               {
                  return _loc15_;
               }
            }
            _loc10_ += 1;
         }
         _loc8_ += 1;
      }
      return undefined;
   }
   static function parseConditionsString(sConditions)
   {
      var _loc3_ = _global.API;
      var _loc4_ = [">","<","=","!"];
      if(sConditions == undefined || sConditions.length == 0)
      {
         return [String(_loc3_.lang.getText("NO_CONDITIONS"))];
      }
      var _loc5_ = sConditions.split("&");
      var _loc6_ = [];
      var _loc7_ = 0;
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
      while(_loc7_ < _loc5_.length)
      {
         _loc5_[_loc7_] = new ank.utils.ExtendedString(_loc5_[_loc7_]).replace(["(",")"],["",""]);
         _loc8_ = _loc5_[_loc7_].split("|");
         _loc9_ = 0;
         for(; _loc9_ < _loc8_.length; _loc9_ += 1)
         {
            _loc10_ = 0;
            while(_loc10_ < _loc4_.length)
            {
               _loc11_ = _loc4_[_loc10_];
               _loc12_ = _loc8_[_loc9_].split(_loc11_);
               if(_loc12_.length > 1)
               {
                  break;
               }
               _loc12_;
               _loc11_;
               _loc10_ += 1;
            }
            if(_loc12_ == undefined)
            {
               _loc11_ = _loc8_[_loc9_].charAt(2);
               if(_loc11_ == "E" || _loc11_ == "X")
               {
                  _loc12_ = _loc8_[_loc9_].split(_loc11_);
               }
               else
               {
                  _loc12_;
               }
            }
            if(_loc12_ != undefined)
            {
               _loc13_ = String(_loc12_[0]);
               _loc14_ = _loc12_[1];
               _loc15_ = "";
               if(!_loc3_.datacenter.Player.isAuthorized)
               {
                  if(_loc13_ == "PZ" || (_loc13_ == "PX" || _loc13_ == "Sc"))
                  {
                     break;
                  }
               }
               switch(_loc13_)
               {
                  case "Ps":
                     _loc14_ = _loc3_.lang.getAlignment(Number(_loc14_)).n;
                     break;
                  case "PS":
                     _loc14_ = _loc14_ != "1" ? _loc3_.lang.getText("MALE") : _loc3_.lang.getText("FEMELE");
                     break;
                  case "PB":
                     _loc14_ = _loc3_.lang.getMapSubAreaName(Number(_loc14_));
                     break;
                  case "Pr":
                     _loc14_ = _loc3_.lang.getAlignmentSpecialization(Number(_loc14_)).n;
                     break;
                  case "Pg":
                     _loc16_ = _loc14_.split(",");
                     if(_loc16_.length == 2)
                     {
                        _loc14_ = _loc3_.lang.getAlignmentFeat(Number(_loc16_[0])).n + " (" + Number(_loc16_[1]) + ")";
                        break;
                     }
                     _loc14_ = _loc3_.lang.getAlignmentFeat(Number(_loc14_)).n;
                     break;
                  case "PG":
                     _loc14_ = _loc3_.lang.getClassText(Number(_loc14_)).sn;
                     break;
                  case "PJ":
                  case "Pj":
                     _loc17_ = _loc14_.split(",");
                     _loc14_ = _loc3_.lang.getJobText(_loc17_[0]).n + (_loc17_[1] != undefined ? " (" + _loc3_.lang.getText("LEVEL_SMALL") + " " + _loc17_[1] + ")" : "");
                     break;
                  case "PM":
                     continue;
                  default:
                     if(_loc20_ !== "PO")
                     {
                        if(_loc20_ !== "CO")
                        {
                           if(_loc20_ !== "FC")
                           {
                              if(_loc20_ !== "FS")
                              {
                                 if(_loc20_ !== "Fz")
                                 {
                                    if(_loc20_ !== "Qa")
                                    {
                                       if(_loc20_ !== "Qf")
                                       {
                                          if(_loc20_ !== "Qs")
                                          {
                                             if(_loc20_ !== "Qo")
                                             {
                                                if(_loc20_ !== "Po")
                                                {
                                                   break;
                                                }
                                                _loc14_ = _loc3_.lang.getMapAreaText(Number(_loc14_)).n;
                                                break;
                                             }
                                             _loc14_ = _loc3_.lang.getQuestObjectiveTypeText(Number(_loc14_));
                                             break;
                                          }
                                          _loc14_ = String(_loc3_.lang.getQuestStepText(Number(_loc14_)).n);
                                          break;
                                       }
                                    }
                                    _loc14_ = _loc3_.lang.getQuestText(Number(_loc14_));
                                    break;
                                 }
                                 _loc14_ = _loc3_.lang.getText("SUMMONED_IN_ALLIED_TEAM") + " : " + _loc3_.lang.getMonstersText(Number(_loc14_)).n;
                                 break;
                              }
                              _loc14_ = String(_loc3_.lang.getStateText(Number(_loc14_)));
                              break;
                           }
                           _loc14_ = _loc3_.lang.getText("HAND_CARD_" + _loc14_);
                           break;
                        }
                        _loc14_ = _loc14_ != "1" ? _loc3_.lang.getText("DO_NOT_OWN_HOUSE") : _loc3_.lang.getText("OWN_HOUSE");
                        break;
                     }
                     _loc18_ = new dofus.datacenter.Item(-1,Number(_loc14_),1,0,"",0);
                     _loc14_ = _loc18_.name;
               }
               _loc13_ = new ank.utils.ExtendedString(_loc13_).replace(["CS","CV","CA","CI","CW","CC","CA","PG","PJ","Pj","PM","PA","PN","PE","<NO>","PS","PR","PL","PK","Pg","Pr","Ps","Pa","PP","PZ","CM","FC","FS","Cm","Cp","Fz","Pk","PB","Po"],_loc3_.lang.getText("ITEM_CHARACTERISTICS").split(","));
               switch(_loc13_)
               {
                  case "BI":
                     _loc19_ = _loc3_.lang.getText("UNUSABLE");
                     break;
                  case "Qa":
                     switch(_loc11_)
                     {
                        case "=":
                           _loc19_ = _loc3_.lang.getText("QUEST_X_IN_PROGRESS",[_loc14_]);
                           break;
                        case "!":
                           _loc19_ = _loc3_.lang.getText("QUEST_X_NOT_IN_PROGRESS",[_loc14_]);
                     }
                  case "Qf":
                     switch(_loc11_)
                     {
                        case "=":
                           _loc19_ = _loc3_.lang.getText("QUEST_X_DONE",[_loc14_]);
                           break;
                        case "!":
                           _loc19_ = _loc3_.lang.getText("QUEST_X_NOT_DONE",[_loc14_]);
                     }
                     break;
                  case "Qs":
                     switch(_loc11_)
                     {
                        case "=":
                           _loc19_ = _loc3_.lang.getText("STEP_X_IN_PROGRESS",[_loc14_]);
                           break;
                        case "!":
                           _loc19_ = _loc3_.lang.getText("STEP_X_NOT_IN_PROGRESS",[_loc14_]);
                           break;
                        case ">":
                           _loc19_ = _loc3_.lang.getText("STEP_X_DONE",[_loc14_]);
                           break;
                        case "<":
                           _loc19_ = _loc3_.lang.getText("STEP_X_NOT_DONE",[_loc14_]);
                     }
                     break;
                  case "Qo":
                     switch(_loc11_)
                     {
                        case ">":
                           _loc19_ = _loc3_.lang.getText("OBJECTIVE_X_DONE",[_loc14_]);
                           break;
                        case "<":
                           _loc19_ = _loc3_.lang.getText("OBJECTIVE_X_NOT_DONE",[_loc14_]);
                     }
                     break;
                  case "PO":
                     switch(_loc11_)
                     {
                        case "=":
                           _loc19_ = _loc3_.lang.getText("ITEM_DO_POSSESS",[_loc14_]);
                           break;
                        case "!":
                           _loc19_ = _loc3_.lang.getText("ITEM_DO_NOT_POSSESS",[_loc14_]);
                           break;
                        case "E":
                           _loc19_ = _loc3_.lang.getText("ITEM_EQUIPPED",[_loc14_]);
                           break;
                        case "X":
                           _loc19_ = _loc3_.lang.getText("ITEM_NOT_EQUIPPED",[_loc14_]);
                     }
                     break;
                  case "Mt":
                     if((_loc20_ = _loc14_) === "0")
                     {
                        _loc11_ = dofus.managers.EffectsManager.replaceText(_loc11_,"=",_loc3_.lang.getText("ITEM_NO"));
                     }
                     _loc13_ = _loc3_.lang.getText("MAP");
                     _loc14_ = _loc3_.lang.getText("DUNGEON");
                     break;
                  case "Ca":
                     _loc13_ = dofus.managers.EffectsManager.formatCaracteristicText("AGILITY",_loc13_.split(",") != undefined ? "ADDITIONNAL" : "BASE");
                     _loc11_ = dofus.managers.EffectsManager.replaceText(_loc11_,"!",_loc3_.lang.getText("ITEM_NO"));
                     _loc14_ = _loc14_.split(",")[0];
                     break;
                  case "Cs":
                     _loc13_ = dofus.managers.EffectsManager.formatCaracteristicText("FORCE",_loc13_.split(",") != undefined ? "ADDITIONNAL" : "BASE");
                     _loc11_ = dofus.managers.EffectsManager.replaceText(_loc11_,"!",_loc3_.lang.getText("ITEM_NO"));
                     _loc14_ = _loc14_.split(",")[0];
                     break;
                  case "Cw":
                     _loc13_ = dofus.managers.EffectsManager.formatCaracteristicText("WISDOM",_loc13_.split(",") != undefined ? "ADDITIONNAL" : "BASE");
                     _loc11_ = dofus.managers.EffectsManager.replaceText(_loc11_,"!",_loc3_.lang.getText("ITEM_NO"));
                     _loc14_ = _loc14_.split(",")[0];
                     break;
                  case "Cc":
                     _loc13_ = dofus.managers.EffectsManager.formatCaracteristicText("CHANCE",_loc13_.split(",") != undefined ? "ADDITIONNAL" : "BASE");
                     _loc11_ = dofus.managers.EffectsManager.replaceText(_loc11_,"!",_loc3_.lang.getText("ITEM_NO"));
                     _loc14_ = _loc14_.split(",")[0];
                     break;
                  case "Ci":
                     _loc13_ = dofus.managers.EffectsManager.formatCaracteristicText("INTELLIGENCE",_loc13_.split(",") != undefined ? "ADDITIONNAL" : "BASE");
                     _loc11_ = dofus.managers.EffectsManager.replaceText(_loc11_,"!",_loc3_.lang.getText("ITEM_NO"));
                     _loc14_ = _loc14_.split(",")[0];
                     break;
                  case "Cv":
                     _loc13_ = dofus.managers.EffectsManager.formatCaracteristicText("VITALITY",_loc13_.split(",") != undefined ? "ADDITIONNAL" : "BASE");
                     _loc11_ = dofus.managers.EffectsManager.replaceText(_loc11_,"!",_loc3_.lang.getText("ITEM_NO"));
                     _loc14_ = _loc14_.split(",")[0];
                     break;
                  default:
                     _loc11_ = dofus.managers.EffectsManager.replaceText(_loc11_,"!",_loc3_.lang.getText("ITEM_NO"));
               }
               if(_loc9_ > 0)
               {
                  _loc15_ = _loc3_.lang.getText("ITEM_OR") + " ";
               }
               else if(_loc7_ > 0)
               {
                  _loc15_ = _loc3_.lang.getText("AND") + " ";
               }
               if(_loc19_ != undefined)
               {
                  dofus.managers.EffectsManager.pushItemCondition(_loc6_,_loc15_ + _loc19_);
               }
               else
               {
                  dofus.managers.EffectsManager.pushItemCondition(_loc6_,_loc15_ + _loc13_ + " " + _loc11_ + " " + _loc14_);
               }
            }
         }
         _loc7_ += 1;
      }
      return _loc6_;
   }
   static function pushItemCondition(aConditions, sCondition)
   {
      aConditions.push(sCondition);
   }
   static function formatCaracteristicText(sCaracteristic, sAdditionnalText)
   {
      return _global.API.lang.getText(sCaracteristic) + " " + _global.API.lang.getText(sAdditionnalText).toLowerCase();
   }
   static function replaceText(sBaseText, sTextToReplace, sTextToReplaceWith)
   {
      return new ank.utils.ExtendedString(sBaseText).replace(sTextToReplace,sTextToReplaceWith);
   }
}
