class dofus.aks.Infos extends dofus.aks.Handler
{
   var addToQueue;
   var api;
   function Infos(oAKS, oAPI)
   {
      super.initialize(oAKS,oAPI);
   }
   function getMaps()
   {
      this.aks.send("IM");
   }
   function sendScreenInfo()
   {
      var _loc2_ = Stage.scaleMode;
      Stage.scaleMode = "noScale";
      var _loc3_;
      switch(Stage.displayState)
      {
         case "normal":
            _loc3_ = "0";
            break;
         case "fullscreen":
            _loc3_ = "1";
            break;
         default:
            _loc3_ = "2";
      }
      this.aks.send("Ir" + Stage.width + ";" + Stage.height + ";" + _loc3_);
      Stage.scaleMode = _loc2_;
   }
   function onInfoMaps(sExtraData)
   {
      var _loc2_ = sExtraData.split("|");
   }
   function onInfoCompass(sExtraData)
   {
      var _loc4_ = sExtraData.split("|");
      var _loc5_ = Number(_loc4_[0]);
      var _loc6_ = Number(_loc4_[1]);
      var _loc7_ = this.api.ui.getUIComponent("MapExplorer");
      if(_loc7_ != undefined)
      {
         _loc7_.select({coordinates:{x:_loc5_,y:_loc6_}});
      }
      if(_global.isNaN(_loc5_) && _global.isNaN(_loc6_))
      {
         this.api.kernel.GameManager.updateCompass(this.api.datacenter.Basics.banner_targetCoords[0],this.api.datacenter.Basics.banner_targetCoords[1],false);
      }
      else
      {
         this.api.kernel.GameManager.updateCompass(_loc5_,_loc6_,true);
      }
   }
   function onInfoCoordinatespHighlight(sExtraData)
   {
      var _loc3_ = [];
      if(sExtraData.length <= 0)
      {
         this.api.datacenter.Basics.aks_infos_highlightCoords = undefined;
         return undefined;
      }
      var _loc4_ = sExtraData.split("|");
      var _loc5_ = 0;
      var _loc6_;
      var _loc7_;
      var _loc8_;
      var _loc9_;
      var _loc10_;
      var _loc11_;
      var _loc12_;
      while(_loc5_ < _loc4_.length)
      {
         _loc6_ = _loc4_[_loc5_].split(";");
         _loc7_ = Number(_loc6_[0]);
         _loc8_ = Number(_loc6_[1]);
         _loc9_ = Number(_loc6_[2]);
         _loc10_ = Number(_loc6_[3]);
         _loc11_ = Number(_loc6_[4]);
         _loc12_ = String(_loc6_[5]);
         _loc3_.push({x:_loc7_,y:_loc8_,mapID:_loc9_,type:_loc10_,playerID:_loc11_,playerName:_loc12_});
         _loc5_ += 1;
      }
      var _loc13_ = this.api.ui.getUIComponent("MapExplorer");
      if(_loc13_ != undefined)
      {
         _loc13_.multipleSelect(_loc3_);
      }
      this.api.datacenter.Basics.aks_infos_highlightCoords = _loc3_;
   }
   function onMessage(sExtraData)
   {
      var _loc4_ = [];
      var _loc5_ = sExtraData.charAt(0);
      var _loc6_ = sExtraData.substr(1).split("|");
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
      while(_loc7_ < _loc6_.length)
      {
         _loc8_ = _loc6_[_loc7_].split(";");
         _loc9_ = String(_loc8_.shift());
         _loc10_ = Number(_loc9_);
         _loc11_ = _loc8_[0].split("~");
         switch(_loc5_)
         {
            case "0":
               _loc12_ = "INFO_CHAT";
               if(!_global.isNaN(_loc10_))
               {
                  _loc13_ = true;
                  switch(_loc10_)
                  {
                     case 21:
                     case 22:
                        _loc14_ = new dofus.datacenter.Item(0,_loc11_[1]);
                        _loc11_ = [_loc11_[0],_loc14_.name];
                        break;
                     case 17:
                        _loc11_ = [_loc11_[0],this.api.lang.getJobText(_loc11_[1]).n];
                        break;
                     case 2:
                        _loc11_ = [this.api.lang.getJobText(Number(_loc11_[0])).n];
                        break;
                     case 3:
                     case 212:
                        _loc11_ = [this.api.lang.getSpellText(Number(_loc11_[0])).n];
                        break;
                     case 54:
                     case 55:
                     case 56:
                        _loc11_[0] = this.api.lang.getQuestText(_loc11_[0]);
                        break;
                     case 65:
                     case 66:
                     case 73:
                        _loc15_ = new dofus.datacenter.Item(0,_loc11_[1]);
                        _loc16_ = new ank.utils.ExtendedString(_loc11_[0]).addMiddleChar(this.api.lang.getConfigText("THOUSAND_SEPARATOR"),3);
                        _loc11_[0] = _loc16_;
                        _loc11_[2] = _loc15_.name;
                        break;
                     case 82:
                     case 83:
                        this.api.kernel.showMessage(this.api.lang.getText("INFORMATIONS"),this.api.lang.getText("INFOS_" + _loc10_,_loc11_),"ERROR_BOX");
                        break;
                     case 120:
                        if(dofus.Constants.SAVING_THE_WORLD)
                        {
                           dofus.SaveTheWorld.getInstance().safeWasBusy();
                           dofus.SaveTheWorld.getInstance().nextAction();
                        }
                        break;
                     case 123:
                        _loc17_ = this.api.kernel.ChatManager.parseInlineItems(this.api.lang.getText("INFOS_" + _loc10_),_loc11_);
                        _loc13_ = false;
                        break;
                     case 150:
                        _loc12_ = "MESSAGE_CHAT";
                        _loc18_ = new dofus.datacenter.Item(0,_loc11_[0]);
                        _loc19_ = [];
                        _loc20_ = 3;
                        while(_loc20_ < _loc11_.length)
                        {
                           _loc19_.push(_loc11_[_loc20_]);
                           _loc20_ += 1;
                        }
                        _loc11_ = [_loc18_.name,_loc11_[1],this.api.lang.getText("OBJECT_CHAT_" + _loc11_[2],_loc19_)];
                        break;
                     case 151:
                        _loc12_ = "WHISP_CHAT";
                        _loc21_ = new dofus.datacenter.Item(0,_loc11_[0]);
                        _loc22_ = [];
                        _loc23_ = 2;
                        while(_loc23_ < _loc11_.length)
                        {
                           _loc22_.push(_loc11_[_loc23_]);
                           _loc23_ += 1;
                        }
                        _loc11_ = [_loc21_.name,this.api.lang.getText("OBJECT_CHAT_" + _loc11_[1],_loc22_)];
                        break;
                     case 8:
                        _loc17_ = this.api.lang.getText("INFOS_8",[new ank.utils.ExtendedString(_loc11_).addMiddleChar(this.api.lang.getConfigText("THOUSAND_SEPARATOR"),3)]);
                        _loc13_ = false;
                        break;
                     case 45:
                        _loc17_ = this.api.lang.getText("INFOS_45",[new ank.utils.ExtendedString(_loc11_).addMiddleChar(this.api.lang.getConfigText("THOUSAND_SEPARATOR"),3)]);
                        _loc13_ = false;
                        break;
                     case 220:
                     case 221:
                        _loc24_ = new dofus.datacenter.Item(0,_loc11_[0]);
                        _loc11_[0] = _loc24_.name;
                        break;
                     case 223:
                        _loc25_ = new dofus.datacenter.Item(0,_loc11_[1]);
                        _loc11_[1] = _loc25_.name;
                        _loc11_[3] = _loc11_[3] != -1 ? this.api.lang.getDungeonText(_loc11_[3]).n : this.api.lang.getText("INVADE");
                        break;
                     case 224:
                        this.api.datacenter.Temporary.hornInviteSender = Number(_loc11_.shift());
                        break;
                     case 84:
                  }
                  if(_loc13_)
                  {
                     _loc17_ = this.api.lang.getText("INFOS_" + _loc10_,_loc11_);
                  }
               }
               else
               {
                  _loc17_ = this.api.lang.getText(_loc9_,_loc11_);
               }
               if(_loc17_ != undefined)
               {
                  _loc4_.push(_loc17_);
               }
               break;
            case "1":
               _loc12_ = "ERROR_CHAT";
               if(!_global.isNaN(_loc10_))
               {
                  _loc26_ = _loc10_.toString(10);
                  switch(_loc10_)
                  {
                     case 16:
                        this.api.electron.makeNotification(_loc29_);
                        break;
                     case 6:
                     case 46:
                     case 49:
                        _loc11_ = [this.api.lang.getJobText(_loc11_[0]).n];
                        break;
                     case 7:
                     case 264:
                        _loc11_ = [this.api.lang.getSpellText(_loc11_[0]).n];
                        break;
                     case 89:
                        if(this.api.config.isStreaming)
                        {
                           _loc26_ = "89_MINICLIP";
                        }
                        if(dofus.Kernel.FAST_SWITCHING_SERVER_REQUEST != undefined)
                        {
                           this.addToQueue({object:this.api.kernel,method:this.api.kernel.onFastServerSwitchSuccess});
                        }
                        break;
                     case 43:
                     case 44:
                     case 60:
                        _loc27_ = new dofus.datacenter.Item(0,_loc11_[0]);
                        _loc11_ = [_loc27_.name];
                        break;
                     case 266:
                        _loc28_ = new dofus.datacenter.QuestObjective(_loc11_[0],false);
                        _loc11_ = [_loc28_.description];
                        break;
                     case 267:
                        _loc11_ = [this.api.lang.getSpellText(Number(_loc11_[0])).n];
                        break;
                     case 282:
                        _loc11_ = [(Number(_loc11_[0]) < 10 ? "0" : "") + _loc11_[0],(Number(_loc11_[1]) < 10 ? "0" : "") + _loc11_[1],_loc11_[2]];
                  }
                  _loc29_ = this.api.lang.getText("ERROR_" + _loc26_,_loc11_);
               }
               else
               {
                  _loc29_ = this.api.lang.getText(_loc9_,_loc11_);
               }
               if(_loc29_ != undefined)
               {
                  _loc4_.push(_loc29_);
               }
               break;
            case "2":
               _loc12_ = "PVP_CHAT";
               if(!_global.isNaN(_loc10_))
               {
                  switch(_loc10_)
                  {
                     case 41:
                        _loc11_ = [this.api.lang.getMapSubAreaText(_loc11_[0]).n,this.api.lang.getMapAreaText(_loc11_[1]).n];
                        break;
                     case 86:
                     case 87:
                     case 88:
                     case 89:
                     case 90:
                        _loc11_[0] = this.api.lang.getMapAreaText(_loc11_[0]).n;
                  }
                  _loc30_ = this.api.lang.getText("PVP_" + _loc10_,_loc11_);
               }
               else
               {
                  _loc30_ = this.api.lang.getText(_loc9_,_loc11_);
               }
               if(_loc30_ != undefined)
               {
                  _loc4_.push(_loc30_);
               }
               break;
            case "3":
               _loc12_ = "GUILD_CHAT";
               switch(_loc10_)
               {
                  case 0:
                  case 1:
                  case 2:
                     _loc11_[0] = _loc8_.join(";");
                     break;
                  case 3:
                     _loc31_ = Number(_loc11_[1]);
                     _loc32_ = this.api.lang.getMapText(_loc31_).x;
                     _loc33_ = this.api.lang.getMapText(_loc31_).y;
                     _loc11_[1] = this.api.kernel.MapsServersManager.getMapName(_loc31_) + " [" + _loc32_ + ", " + _loc33_ + "]";
               }
               _loc34_ = this.api.lang.getText("GUILD_" + _loc10_,_loc11_);
               if(_loc34_ != undefined)
               {
                  _loc4_.push(_loc34_);
               }
         }
         _loc7_ += 1;
      }
      var _loc35_ = _loc4_.join(" ");
      if(_loc35_ != "")
      {
         this.api.kernel.showMessage(undefined,_loc35_,_loc12_);
      }
   }
   function onQuantity(sExtraData)
   {
      var _loc3_ = sExtraData.split("|");
      var _loc4_ = _loc3_[0];
      var _loc5_ = _loc3_[1];
      var _loc6_ = _loc5_ > 0;
      var _loc7_ = (!_loc6_ ? " " : "+") + String(_loc5_);
      this.api.gfx.addSpritePoints(_loc4_,_loc7_,dofus.Constants.CLIP_POINT_TYPE_QUANTITY);
   }
   function onObject(sExtraData)
   {
      var _loc3_ = sExtraData.split("|");
      var _loc4_ = _loc3_[0];
      if(_loc4_ != this.api.datacenter.Player.ID && this.api.gfx.spriteHandler.isPlayerSpritesHidden)
      {
         return undefined;
      }
      var _loc5_ = _loc3_[1].charAt(0) == "+";
      var _loc6_ = _loc3_[1].substr(1);
      var _loc7_ = _loc6_ != "" ? new dofus.datacenter.Item(0,_loc6_,1) : undefined;
      if(!this.api.datacenter.Basics.isCraftLooping)
      {
         this.api.gfx.addSpriteOverHeadItem(_loc4_,"craft",dofus.graphics.battlefield.CraftResultOverHead,[_loc5_,_loc7_],2000);
      }
   }
   function onLifeRestoreTimerStart(sExtraData)
   {
      var _loc4_ = Number(sExtraData);
      _global.clearInterval(this.api.datacenter.Basics.aks_infos_lifeRestoreInterval);
      var _loc5_;
      if(!_global.isNaN(_loc4_))
      {
         _loc5_ = this.api.datacenter.Player;
         this.api.datacenter.Basics.aks_infos_lifeRestoreInterval = _global.setInterval(_loc5_,"updateLP",_loc4_,1);
      }
   }
   function onAutopilotPath(sExtraData)
   {
      var _loc4_ = [];
      var _loc5_;
      var _loc6_;
      var _loc7_;
      if(sExtraData.length > 0)
      {
         _loc5_ = sExtraData.split("|");
         _loc6_ = 0;
         while(_loc6_ < _loc5_.length)
         {
            _loc7_ = _loc5_[_loc6_].split(";");
            if(!_global.isNaN(Number(_loc7_[0])) && !_global.isNaN(Number(_loc7_[1])))
            {
               _loc4_.push({x:Number(_loc7_[0]),y:Number(_loc7_[1]),type:Number(_loc7_[2])});
            }
            _loc6_ += 1;
         }
      }
      this.api.datacenter.Basics.autopilotPath = _loc4_;
      var _loc8_ = this.api.ui.getUIComponent("Banner");
      var _loc9_;
      var _loc10_;
      var _loc11_;
      if(_loc8_ != undefined)
      {
         _loc9_ = _loc8_._mcXtra;
         if(_loc9_ != undefined && _loc9_.updateAutopilotPath != undefined)
         {
            _loc9_.updateAutopilotPath(_loc4_);
         }
         if(_loc8_.chat != undefined)
         {
            _loc10_ = _loc8_.chat.miniMapReplacementPanel;
            if(_loc10_ != undefined && _loc10_.updateAutopilotPath != undefined)
            {
               _loc10_.updateAutopilotPath(_loc4_);
            }
            _loc11_ = _loc8_.chat.shortcutsReplacementPanel;
            if(_loc11_ != undefined && _loc11_.miniMap != undefined && _loc11_.miniMap.updateAutopilotPath != undefined)
            {
               _loc11_.miniMap.updateAutopilotPath(_loc4_);
            }
         }
      }
      var _loc12_ = this.api.ui.getUIComponent("MapExplorer");
      if(_loc12_ != undefined && _loc12_.updateAutopilotPath != undefined)
      {
         _loc12_.updateAutopilotPath(_loc4_);
      }
   }
   function onLifeRestoreTimerFinish(sExtraData)
   {
      var _loc4_ = Number(sExtraData);
      _global.clearInterval(this.api.datacenter.Basics.aks_infos_lifeRestoreInterval);
      if(_loc4_ > 0)
      {
         this.api.kernel.showMessage(undefined,this.api.lang.getText("YOU_RESTORE_LIFE",[_loc4_]),"INFO_CHAT");
      }
   }
}
