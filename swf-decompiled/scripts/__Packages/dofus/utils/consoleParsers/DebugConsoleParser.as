class dofus.utils.consoleParsers.DebugConsoleParser extends dofus.utils.consoleParsers.AbstractConsoleParser
{
   function DebugConsoleParser(oAPI)
   {
      super();
      this.initialize(oAPI);
   }
   function initialize(oAPI)
   {
      super.initialize(oAPI);
   }
   function process(sCmds)
   {
      super.process(sCmds);
      var _loc4_ = sCmds.split(dofus.aks.Basics.MULTIPLE_ADMIN_COMMANDS_SPLIT_STR);
      var _loc5_ = 0;
      var _loc6_;
      while(_loc5_ < _loc4_.length)
      {
         _loc6_ = _loc4_[_loc5_];
         this.processOneAdminCommand(_loc6_);
         _loc5_ = _loc5_ + 1;
      }
   }
   function processOneAdminCommand(sCmd)
   {
      var _loc3_;
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
      var _loc46_;
      var _loc47_;
      var _loc48_;
      var _loc49_;
      var _loc50_;
      var _loc51_;
      var _loc52_;
      var _loc53_;
      var _loc54_;
      var _loc55_;
      var _loc56_;
      var _loc57_;
      var _loc58_;
      var _loc59_;
      var _loc60_;
      var _loc61_;
      var _loc62_;
      var _loc63_;
      var _loc64_;
      var _loc65_;
      var _loc66_;
      var _loc67_;
      var _loc68_;
      var _loc69_;
      var _loc70_;
      var _loc71_;
      var _loc72_;
      var _loc73_;
      var _loc74_;
      var _loc75_;
      var _loc76_;
      var _loc77_;
      var _loc78_;
      var _loc79_;
      var _loc80_;
      var _loc81_;
      var _loc82_;
      var _loc83_;
      var _loc84_;
      var _loc85_;
      var _loc86_;
      var _loc87_;
      var _loc88_;
      var _loc89_;
      var _loc90_;
      var _loc91_;
      var _loc92_;
      var _loc93_;
      var _loc94_;
      var _loc95_;
      var _loc96_;
      var _loc97_;
      var _loc98_;
      var _loc99_;
      var _loc100_;
      var _loc101_;
      if(sCmd.charAt(0) == "/")
      {
         _loc3_ = sCmd.split(" ");
         _loc4_ = _loc3_[0].substr(1).toUpperCase();
         _loc3_.splice(0,1);
         switch(_loc4_)
         {
            case "ADMINSELECT":
               if(_loc3_[0] == undefined || _loc3_[0].length == 0)
               {
                  this.showMessage(undefined,"Syntax : /adminselect [pseudos]","DEBUG_LOG");
                  return undefined;
               }
               _loc5_ = this.api.kernel.AdminManager.getAdminPopupMenu(_loc3_[0],false);
               _loc5_.show(_root._xmouse,_root._ymouse,true);
               break;
            case "CONSOLEMAXSIZE":
               if(this.api.kernel.OptionsManager.getOption("DebugSizeIndex") != dofus.graphics.gapi.ui.Debug.CONSOLE_MAXSIZE)
               {
                  this.api.kernel.OptionsManager.setOption("DebugSizeIndex",dofus.graphics.gapi.ui.Debug.CONSOLE_MAXSIZE);
                  _loc6_ = dofus.graphics.gapi.ui.Debug(this.api.ui.getUIComponent("Debug"));
                  if(_loc6_ != undefined)
                  {
                     _loc6_.applySizeIndex(false);
                  }
               }
               break;
            case "CONSOLEMINIMIZED":
               if(this.api.kernel.OptionsManager.getOption("DebugSizeIndex") != dofus.graphics.gapi.ui.Debug.CONSOLE_MINIMIZED)
               {
                  this.api.kernel.OptionsManager.setOption("DebugSizeIndex",dofus.graphics.gapi.ui.Debug.CONSOLE_MINIMIZED);
                  _loc7_ = dofus.graphics.gapi.ui.Debug(this.api.ui.getUIComponent("Debug"));
                  if(_loc7_ != undefined)
                  {
                     _loc7_.applySizeIndex(false);
                  }
               }
               break;
            case "CONSOLEMEDIUM":
               if(this.api.kernel.OptionsManager.getOption("DebugSizeIndex") != dofus.graphics.gapi.ui.Debug.CONSOLE_MEDIUM)
               {
                  this.api.kernel.OptionsManager.setOption("DebugSizeIndex",dofus.graphics.gapi.ui.Debug.CONSOLE_MEDIUM);
                  _loc8_ = dofus.graphics.gapi.ui.Debug(this.api.ui.getUIComponent("Debug"));
                  if(_loc8_ != undefined)
                  {
                     _loc8_.applySizeIndex(false);
                  }
               }
               break;
            case "CHATPLUS":
               _loc9_ = dofus.graphics.gapi.controls.Chat(this.api.ui.getUIComponent("Banner").chat);
               if(_loc9_ != undefined)
               {
                  _loc9_.open(false);
               }
               break;
            case "CHATMINUS":
               _loc10_ = dofus.graphics.gapi.controls.Chat(this.api.ui.getUIComponent("Banner").chat);
               if(_loc10_ != undefined)
               {
                  _loc10_.open(true);
               }
               break;
            case "LANGFILE":
               _loc11_ = Number(_loc3_[0]);
               if(_global.isNaN(_loc11_))
               {
                  this.showMessage(undefined,"I need a valid data bank ID","DEBUG_LOG");
                  break;
               }
               this.showMessage(undefined,_loc3_[1] + " lang file size : " + this.api.lang.getLangFileSize(_loc11_,_loc3_[1]) + " octets","DEBUG_LOG");
               break;
            case "POINTSPRITE":
               this.api.kernel.TipsManager.pointSprite(-1,Number(_loc3_[0]));
               break;
            case "VIEWFIGHTRESULT":
               _loc12_ = Number(sCmd.substr(_loc4_.length + 2));
               if(_global.isNaN(_loc12_) || _loc12_ < 1)
               {
                  this.showMessage(undefined,"Syntax : /fightresult [1" + (this.api.datacenter.Game.resultsArray.length <= 1 ? "" : " - " + this.api.datacenter.Game.resultsArray.length) + "]","DEBUG_LOG");
                  return undefined;
               }
               if(this.api.datacenter.Basics.currentSessionFightCount == -1)
               {
                  this.showMessage(undefined,"You didn\'t do any fights yet.","DEBUG_LOG");
               }
               else if(_loc12_ > this.api.datacenter.Game.resultsArray.length)
               {
                  this.showMessage(undefined,"This fight does not exist, try between 1 and " + this.api.datacenter.Game.resultsArray.length + ", where 1 is the most recent.","DEBUG_LOG");
               }
               else
               {
                  this.api.kernel.GameManager.showEndPanel(_loc12_ - 1,true);
               }
               break;
            case "LISTSPRITES":
               _loc13_ = this.api.gfx.spriteHandler.getSprites().getItems();
               for(var k in _loc13_)
               {
                  _loc14_ = _loc13_[k];
                  this.showMessage(undefined,"Sprite " + _loc14_.gfxFile + " (cell : " + _loc14_.cellNum + ")","DEBUG_LOG");
               }
               break;
            case "LISTPICTOS":
               _loc15_ = this.api.gfx.mapHandler.getCellsData();
               for(var k in _loc15_)
               {
                  if(_loc15_[k].layerObject1Num != undefined && (!_global.isNaN(_loc15_[k].layerObject1Num) && _loc15_[k].layerObject1Num > 0))
                  {
                     this.showMessage(undefined,"Picto " + _loc15_[k].layerObject1Num,"DEBUG_LOG");
                  }
                  if(_loc15_[k].layerObject2Num != undefined && (!_global.isNaN(_loc15_[k].layerObject2Num) && _loc15_[k].layerObject2Num > 0))
                  {
                     this.showMessage(undefined,"Picto " + _loc15_[k].layerObject2Num,"DEBUG_LOG");
                  }
               }
               break;
            case "POINTPICTO":
               this.api.kernel.TipsManager.pointPicto(-1,Number(_loc3_[0]));
               break;
            case "SAVETHEWORLD":
               if(dofus.Constants.SAVING_THE_WORLD)
               {
                  dofus.SaveTheWorld.execute();
               }
               else
               {
                  this.showMessage(undefined,this.api.lang.getText("UNKNOW_COMMAND",[_loc4_]),"DEBUG_ERROR");
               }
               break;
            case "STOPSAVETHEWORLD":
               if(dofus.Constants.SAVING_THE_WORLD)
               {
                  dofus.SaveTheWorld.stop();
               }
               else
               {
                  this.showMessage(undefined,this.api.lang.getText("UNKNOW_COMMAND",[_loc4_]),"DEBUG_ERROR");
               }
               break;
            case "NEXTSAVE":
               if(dofus.Constants.SAVING_THE_WORLD)
               {
                  dofus.SaveTheWorld.getInstance().nextAction();
               }
               else
               {
                  this.showMessage(undefined,this.api.lang.getText("UNKNOW_COMMAND",[_loc4_]),"DEBUG_ERROR");
               }
               break;
            case "SEARCHNPC":
               _loc16_ = sCmd.substr(_loc4_.length + 2);
               if(_loc16_ == undefined || _loc16_.length < 2)
               {
                  this.showMessage(undefined,"Syntax : /searchnpc [name]","DEBUG_LOG");
                  return undefined;
               }
               _loc17_ = "";
               _loc17_ += "Looking for npc : " + _loc16_;
               _loc18_ = this.api.lang.getNonPlayableCharactersTexts();
               for(var i in _loc18_)
               {
                  if(_loc18_[i].n.toUpperCase().indexOf(_loc16_.toUpperCase()) != -1)
                  {
                     _loc17_ += "\n " + _loc18_[i].n + " : " + i;
                  }
               }
               this.showMessage(undefined,_loc17_,"DEBUG_LOG");
               break;
            case "SEARCHBREED":
               _loc19_ = sCmd.substr(_loc4_.length + 2);
               if(_loc19_ == undefined || _loc19_.length < 2)
               {
                  this.showMessage(undefined,"Syntax : /searchbreed [name]","DEBUG_LOG");
                  return undefined;
               }
               _loc20_ = "";
               _loc20_ += "Looking for breed : " + _loc19_;
               _loc21_ = this.api.lang.getAllClassText();
               for(var i in _loc21_)
               {
                  if(_loc21_[i].sn.toUpperCase().indexOf(_loc19_.toUpperCase()) != -1)
                  {
                     _loc20_ += "\n " + _loc21_[i].sn + " : " + i;
                  }
               }
               this.showMessage(undefined,_loc20_,"DEBUG_LOG");
               break;
            case "SEARCHALIGNMENT":
               _loc22_ = sCmd.substr(_loc4_.length + 2);
               if(_loc22_ == undefined || _loc22_.length < 2)
               {
                  this.showMessage(undefined,"Syntax : /searchalignment [name]","DEBUG_LOG");
                  return undefined;
               }
               _loc23_ = "";
               _loc23_ += "Looking for alignment : " + _loc22_;
               _loc24_ = this.api.lang.getAlignments();
               for(var i in _loc24_)
               {
                  if(_loc24_[i].n.toUpperCase().indexOf(_loc22_.toUpperCase()) != -1)
                  {
                     _loc23_ += "\n " + _loc24_[i].n + " : " + i;
                  }
               }
               this.showMessage(undefined,_loc23_,"DEBUG_LOG");
               break;
            case "SEARCHITEM":
               _loc25_ = sCmd.substr(_loc4_.length + 2);
               if(_loc25_ == undefined || _loc25_.length < 2)
               {
                  this.showMessage(undefined,"Syntax : /searchitem [name]","DEBUG_LOG");
                  return undefined;
               }
               _loc26_ = "";
               _loc26_ += "Looking for item : " + _loc25_;
               _loc27_ = this.api.lang.getItemUnics();
               for(var i in _loc27_)
               {
                  if(_loc27_[i].n.toUpperCase().indexOf(_loc25_.toUpperCase()) != -1)
                  {
                     _loc26_ += "\n " + _loc27_[i].n + " : " + i + " (gfx : " + _loc27_[i].t + "/" + _loc27_[i].g + ".swf)";
                  }
               }
               this.showMessage(undefined,_loc26_,"DEBUG_LOG");
               break;
            case "SEARCHJOB":
               _loc28_ = sCmd.substr(_loc4_.length + 2);
               if(_loc28_ == undefined || _loc28_.length < 2)
               {
                  this.showMessage(undefined,"Syntax : /searchjob [name]","DEBUG_LOG");
                  return undefined;
               }
               _loc29_ = "";
               _loc29_ += "Looking for job : " + _loc28_;
               _loc30_ = this.api.lang.getAllJobsText();
               for(var i in _loc30_)
               {
                  _loc31_ = _loc30_[i];
                  if(!(_global.isNaN(_loc31_.g) || _loc31_.g < 1))
                  {
                     if(_loc31_.n.toUpperCase().indexOf(_loc28_.toUpperCase()) != -1)
                     {
                        _loc29_ += "\n " + _loc31_.n + " : " + i;
                     }
                  }
               }
               this.showMessage(undefined,_loc29_,"DEBUG_LOG");
               break;
            case "SEARCHMONSTER":
               _loc32_ = sCmd.substr(_loc4_.length + 2);
               if(_loc32_ == undefined || _loc32_.length < 2)
               {
                  this.showMessage(undefined,"Syntax : /searchmonster [name]","DEBUG_LOG");
                  return undefined;
               }
               _loc33_ = "";
               _loc33_ += "Looking for monster : " + _loc32_;
               _loc34_ = this.api.lang.getMonsters();
               for(var i in _loc34_)
               {
                  if(_loc34_[i].n.toUpperCase().indexOf(_loc32_.toUpperCase()) != -1)
                  {
                     _loc33_ += "\n " + _loc34_[i].n + " : " + i + " (gfx : " + _loc34_[i].g + ")";
                  }
               }
               this.showMessage(undefined,_loc33_,"DEBUG_LOG");
               break;
            case "SEARCHSUBAREA":
               _loc35_ = sCmd.substr(_loc4_.length + 2);
               if(_loc35_ == undefined || _loc35_.length < 2)
               {
                  this.showMessage(undefined,"Syntax : /searchsubarea [name]","DEBUG_LOG");
                  return undefined;
               }
               _loc36_ = "";
               _loc36_ += "Looking for subarea : " + _loc35_;
               _loc37_ = this.api.lang.getMapSubAreas();
               for(var i in _loc37_)
               {
                  if(_loc37_[i].n.toUpperCase().indexOf(_loc35_.toUpperCase()) != -1)
                  {
                     _loc36_ += "\n " + _loc37_[i].n + " : " + i;
                  }
               }
               this.showMessage(undefined,_loc36_,"DEBUG_LOG");
               break;
            case "SEARCHSPELL":
               _loc38_ = sCmd.substr(_loc4_.length + 2);
               if(_loc38_ == undefined || _loc38_.length < 2)
               {
                  this.showMessage(undefined,"Syntax : /searchspell [name]","DEBUG_LOG");
                  return undefined;
               }
               _loc39_ = "";
               _loc39_ += "Looking for spell : " + _loc38_;
               _loc40_ = this.api.lang.getSpells();
               for(var i in _loc40_)
               {
                  if(_loc40_[i].n.toUpperCase().indexOf(_loc38_.toUpperCase()) != -1)
                  {
                     _loc39_ += "\n " + _loc40_[i].n + " : " + i;
                  }
               }
               this.showMessage(undefined,_loc39_,"DEBUG_LOG");
               break;
            case "SEARCHQUEST":
               _loc41_ = sCmd.substr(_loc4_.length + 2);
               if(_loc41_ == undefined || _loc41_.length < 2)
               {
                  this.showMessage(undefined,"Syntax : /searchquest [name]","DEBUG_LOG");
                  return undefined;
               }
               _loc42_ = "";
               _loc42_ += "Looking for quest : " + _loc41_;
               _loc43_ = this.api.lang.getQuests();
               for(var i in _loc43_)
               {
                  if(_loc43_[i].toUpperCase().indexOf(_loc41_.toUpperCase()) != -1)
                  {
                     _loc42_ += "\n " + _loc43_[i] + " : " + i;
                  }
               }
               this.showMessage(undefined,_loc42_,"DEBUG_LOG");
               break;
            case "TACTIC":
               if(this.api.datacenter.Player.isAuthorized || this.api.datacenter.Game.isFight)
               {
                  _loc44_ = !this.api.datacenter.Game.isTacticMode;
                  this.api.datacenter.Game.isTacticMode = _loc44_;
                  this.api.gfx.activateTacticMode(this.api,_loc44_);
                  this.api.ui.getUIComponent("FightOptionButtons")._btnTactic.selected = _loc44_;
               }
               break;
            case "VERSION":
            case "VER":
            case "ABOUT":
               _loc45_ = "------------------------------------------------------\n";
               _loc45_ += "<b>DOFUS RETRO Client v" + dofus.Constants.VERSION + "." + dofus.Constants.SUBVERSION + "." + dofus.Constants.SUBSUBVERSION + "</b>";
               if(dofus.Constants.BETAVERSION > 0)
               {
                  _loc45_ += " <b><font color=\"#FF0000\">BETA VERSION " + dofus.Constants.BETAVERSION + "</font></b>";
               }
               _loc45_ += "\n(c) ANKAMA GAMES (" + dofus.Constants.VERSIONDATE + ")\n";
               _loc45_ += "Flash player " + System.capabilities.version + "\n";
               _loc45_ += "------------------------------------------------------";
               this.showMessage(undefined,_loc45_,"DEBUG_LOG");
               break;
            case "INFOS":
               _loc46_ = "Svr:";
               _loc46_ += "\nNb:";
               _loc46_ += "\n Map  : " + String(this.api.datacenter.Game.playerCount);
               _loc46_ += "\n Cell : " + this.api.datacenter.Map.data[this.api.datacenter.Player.data.cellNum].spriteOnCount;
               _loc46_ += "\nDataServers:";
               _loc47_ = 0;
               while(_loc47_ < this.api.config.dataServers.length)
               {
                  _loc46_ += "\n host : " + this.api.config.dataServers[_loc47_].url;
                  _loc47_ = _loc47_ + 1;
               }
               _loc46_ += "\n l   : " + this.api.config.language + " (" + this.api.lang.getLangVersion() + " & " + this.api.lang.getXtraVersion() + ")";
               this.showMessage(undefined,_loc46_,"DEBUG_LOG");
               break;
            case "ZOOM":
               this.api.kernel.GameManager.zoomGfx(_loc3_[0],_loc3_[1],_loc3_[2]);
               break;
            case "TIMERSCOUNT":
               this.showMessage(undefined,String(ank.utils.Timer.getTimersCount()),"DEBUG_LOG");
               break;
            case "VARS":
               this.showMessage(undefined,this.api.kernel.TutorialManager.vars,"DEBUG_LOG");
               break;
            case "MOUNT":
               _loc48_ = this.api.gfx.getSprite(this.api.datacenter.Player.ID);
               if(!_loc48_.isMounting)
               {
                  _loc49_ = _loc3_[0] == undefined ? "7002.swf" : _loc3_[0] + ".swf";
                  _loc50_ = _loc3_[1] == undefined ? "10.swf" : _loc3_[1] + ".swf";
                  _loc51_ = new ank.battlefield.datacenter.Mount(dofus.Constants.CLIPS_PERSOS_PATH + _loc49_,dofus.Constants.CHEVAUCHOR_PATH + _loc50_);
                  this.api.gfx.mountSprite(this.api.datacenter.Player.ID,_loc51_);
               }
               else
               {
                  this.api.gfx.unmountSprite(this.api.datacenter.Player.ID);
               }
               break;
            case "SCALE":
               this.api.gfx.setSpriteScale(this.api.datacenter.Player.ID,_loc3_[0],_loc3_.length != 2 ? _loc3_[0] : _loc3_[1]);
               break;
            case "ANIM":
               if(dofus.Constants.DEBUG)
               {
                  if(_loc3_.length > 1)
                  {
                     this.api.gfx.setSpriteLoopAnim(this.api.datacenter.Player.ID,_loc3_[0],_loc3_[1]);
                  }
                  else
                  {
                     this.api.gfx.setSpriteAnim(this.api.datacenter.Player.ID,_loc3_.join(""));
                  }
               }
               break;
            case "C":
               if(dofus.Constants.DEBUG)
               {
                  _loc52_ = _loc3_[0];
                  _loc3_.splice(0,1);
                  switch(_loc52_)
                  {
                     case ">":
                        this.api.network.send(_loc3_.join(" "));
                        break;
                     case "<":
                        this.api.network.processCommand(_loc3_.join(" "));
                  }
               }
               break;
            case "D":
               if(dofus.Constants.DEBUG)
               {
                  _loc53_ = _loc3_[0];
                  _loc3_.splice(0,1);
                  switch(_loc53_)
                  {
                     case ">":
                        this.api.network.send(_loc3_.join(" "),false,undefined,false,true);
                        break;
                     case "<":
                        this.api.network.processCommand(_loc3_.join(" "));
                  }
               }
               break;
            case "DECRYPTFIGHTCELLS":
               if(!_loc3_[0] || (_loc3_[0].length == 0 || _loc3_[0].length % 2 != 0))
               {
                  this.showMessage(undefined,"I need a valid fight cells string !","DEBUG_LOG");
                  return undefined;
               }
               this.showMessage(undefined,"Full string : " + _loc3_[0],"DEBUG_LOG");
               _loc54_ = 0;
               while(_loc54_ < _loc3_[0].length)
               {
                  _loc55_ = ank.utils.Compressor.decode64(_loc3_[0].charAt(_loc54_)) << 6;
                  _loc55_ += ank.utils.Compressor.decode64(_loc3_[0].charAt(_loc54_ + 1));
                  this.showMessage(undefined,_loc3_[0].charAt(_loc54_) + _loc3_[0].charAt(_loc54_ + 1) + " : " + _loc55_,"DEBUG_LOG");
                  _loc54_ += 2;
               }
               break;
            case "CRYPTCELLS":
               if(_loc3_[0] == undefined)
               {
                  this.showMessage(undefined,"I need an ID chain","DEBUG_LOG");
                  return undefined;
               }
               _loc56_ = _loc3_[0].split(",");
               _loc57_ = 0;
               while(_loc57_ < _loc56_.length)
               {
                  _loc58_ = Number(_loc56_[_loc57_]);
                  if(_global.isNaN(_loc58_) || _loc58_ == undefined)
                  {
                     this.showMessage(undefined,"Number not valid : " + _loc58_,"DEBUG_LOG");
                  }
                  else
                  {
                     _loc59_ = Math.floor(_loc58_ / 64);
                     _loc60_ = _loc58_ % 64;
                     _loc61_ = ank.utils.Compressor.encode64(_loc59_) + ank.utils.Compressor.encode64(_loc60_);
                     this.showMessage(undefined,_loc58_ + " : " + _loc61_,"DEBUG_LOG");
                  }
                  _loc57_ = _loc57_ + 1;
               }
               break;
            case "DEBUGZAAP":
               dofus.ZaapConnect.newInstance();
               break;
            case "PRINTJAILDIALOG":
               _loc62_ = this.api.kernel.ChatManager.getJailDialog();
               if(_loc62_.length == 0)
               {
                  this.showMessage(undefined,"No jail dialog found","DEBUG_LOG");
               }
               else
               {
                  this.showMessage(undefined,_loc62_,"DEBUG_LOG");
               }
               break;
            case "SENDMODREPORTS":
               if(!this.api.electron.enabled)
               {
                  this.showMessage(undefined,"This feature is not compatible on a Flash Projector","ERROR_CHAT");
                  return undefined;
               }
               this.api.electron.modReportsSendDayReportsDocs(_loc3_[0]);
               break;
            case "MERGEMODREPORTS":
               if(!this.api.electron.enabled)
               {
                  this.showMessage(undefined,"This feature is not compatible on a Flash Projector","ERROR_CHAT");
                  return undefined;
               }
               this.api.electron.modReportsMergeDayReportsDocs(_loc3_[0]);
               break;
            case "PRINTMODREPORTS":
               if(!this.api.electron.enabled)
               {
                  this.showMessage(undefined,"This feature is not compatible on a Flash Projector","ERROR_CHAT");
                  return undefined;
               }
               this.api.electron.modReportsPrintPendingReports();
               break;
            case "MAKEREPORT":
               if(!this.api.electron.enabled)
               {
                  this.showMessage(undefined,"This feature is not compatible on a Flash Projector","ERROR_CHAT");
                  return undefined;
               }
               if(!dofus.managers.AdminManager.getInstance().isExecutingBatch)
               {
                  this.showMessage(undefined,"You can\'t do this out of a batch execution.","ERROR_CHAT");
                  return undefined;
               }
               _loc63_ = sCmd.split("|");
               _loc64_ = _loc63_[0].substring(_loc4_.length + 2);
               _loc65_ = _loc63_[1] == "allaccounts";
               _loc66_ = _loc63_[2];
               _loc67_ = _loc63_[3].split(",");
               if(_loc64_ == undefined || (_loc64_.length < 1 || (_loc66_ == undefined || _loc66_.length < 1)))
               {
                  this.showMessage(undefined,"/makereport &lt;target pseudos|\'allaccounts\'|reason|[autocomplete action]&gt;","DEBUG_LOG");
                  return undefined;
               }
               if(_loc67_ != undefined)
               {
                  _loc70_ = 0;
                  while(_loc70_ < _loc67_.length)
                  {
                     _loc71_ = _loc67_[_loc70_];
                     switch(_loc71_)
                     {
                        case "chatmessage":
                           _loc68_ = this.api.kernel.GameManager.lastClickedMessage;
                           break;
                        case "jaildialog":
                           _loc69_ = this.api.kernel.ChatManager.getJailDialog();
                     }
                     _loc70_ = _loc70_ + 1;
                  }
               }
               _loc72_ = dofus.graphics.gapi.ui.MakeReport(this.api.ui.getUIComponent("MakeReport"));
               if(_loc72_ == undefined)
               {
                  this.api.datacenter.Temporary.Report = {};
                  _loc73_ = this.api.datacenter.Temporary.Report;
                  _loc73_.currentTargetPseudos = _loc64_;
                  _loc73_.currentTargetIsAllAccounts = _loc65_;
                  _loc73_.targetPseudos = _loc64_;
                  _loc73_.description = _loc68_;
                  _loc73_.jailDialog = _loc69_;
                  _loc73_.isAllAccounts = _loc65_;
                  _loc73_.reason = _loc66_;
               }
               else
               {
                  _loc74_ = this.api.datacenter.Temporary.Report;
                  _loc74_.currentTargetPseudos = _loc64_;
                  _loc74_.currentTargetIsAllAccounts = _loc65_;
                  _loc74_.targetPseudos += "," + _loc64_;
                  _loc74_.description = _loc68_;
               }
               this.api.network.Basics.askReportInfos(1,_loc64_,_loc65_);
               break;
            case "RETROCONSOLE":
               if(!this.api.electron.enabled)
               {
                  this.showMessage(undefined,"Does not work on a Flash Projector","DEBUG_ERROR");
                  break;
               }
               dofus.Electron.retroConsoleOpen();
               break;
            case "FASTSERVERSWITCH":
               _loc75_ = Number(_loc3_[0]);
               if(_global.isNaN(_loc75_) || _loc75_ == undefined)
               {
                  this.showMessage(undefined,"I need a valid server ID !","DEBUG_LOG");
                  return undefined;
               }
               _loc76_ = this.api.datacenter.Player.Name;
               if(_loc76_ == undefined)
               {
                  this.showMessage(undefined,"You have to be in game to do this","DEBUG_LOG");
                  return undefined;
               }
               _loc77_ = {};
               _loc77_.serverId = _loc75_;
               _loc77_.playerName = _loc76_;
               dofus.Kernel.FAST_SWITCHING_SERVER_REQUEST = _loc77_;
               this.api.network.askCharacterSwitchTicket();
               break;
            case "CLEAR":
               this.api.ui.getUIComponent("Debug").clear();
               this.api.electron.retroConsoleClear();
               break;
            case "WIDESCREENPANEL":
               if(this.api.electron.enabled)
               {
                  _loc78_ = Number(_loc3_[0]);
                  if(_loc3_[0] == undefined || (_global.isNaN(_loc78_) || (_loc78_ < 0 || _loc78_ > 1)))
                  {
                     this.showMessage(undefined,"/widescreenpanel &lt;0 (chat) | 1 (console)","DEBUG_LOG");
                     return undefined;
                  }
                  this.api.electron.setWidescreenPanelId(_loc78_);
               }
               else
               {
                  this.showMessage(undefined,"Does not work on a Flash Projector","DEBUG_ERROR");
               }
               break;
            case "FILEOUTPUT":
               if(this.api.electron.enabled)
               {
                  _loc79_ = Number(_loc3_[0]);
                  if(_loc3_[0] == undefined || (_global.isNaN(_loc79_) || (_loc79_ < 0 || _loc79_ > 2)))
                  {
                     this.showMessage(undefined,"/fileoutput &lt;0 (disabled) | 1 (enabled) | 2 (full)&gt;","DEBUG_LOG");
                     return undefined;
                  }
                  _loc80_ = "";
                  switch(_loc79_)
                  {
                     case 0:
                        _loc80_ = "Disabled";
                        break;
                     case 1:
                        _loc80_ = "Enabled";
                        break;
                     case 2:
                        _loc80_ = "Enabled (full)";
                  }
                  dofus.graphics.gapi.ui.Debug.FILE_OUTPUT_STATE = _loc79_;
                  this.showMessage(undefined,"File Output (Console) : " + _loc80_,"DEBUG_LOG");
               }
               else
               {
                  this.showMessage(undefined,"Does not work on a Flash Projector","DEBUG_ERROR");
               }
               break;
            case "LOGDISCO":
               if(_loc3_[0] == "1")
               {
                  this.api.datacenter.Game.isLoggingMapDisconnections = true;
               }
               else if(_loc3_[0] == "0")
               {
                  this.api.datacenter.Game.isLoggingMapDisconnections = false;
               }
               else
               {
                  this.api.datacenter.Game.isLoggingMapDisconnections = !this.api.datacenter.Game.isLoggingMapDisconnections;
               }
               this.showMessage(undefined,"LOG DISCONNECTIONS ON MAP : " + this.api.datacenter.Game.isLoggingMapDisconnections,"DEBUG_LOG");
               break;
            case "FORCEFLASHCHAT":
               if(_loc3_[0] == "1")
               {
                  this.api.datacenter.Basics.forceFlashChat = true;
               }
               else if(_loc3_[0] == "0")
               {
                  this.api.datacenter.Basics.forceFlashChat = false;
               }
               else
               {
                  this.api.datacenter.Basics.forceFlashChat = !this.api.datacenter.Basics.forceFlashChat;
               }
               this.showMessage(undefined,"FORCE FLASH CHAT : " + this.api.datacenter.Basics.forceFlashChat,"DEBUG_LOG");
               _loc81_ = dofus.graphics.gapi.ui.Banner(this.api.ui.getUIComponent("Banner"));
               if(_loc81_ != undefined)
               {
                  _loc82_ = this.api.datacenter.Basics.forceFlashChat || (!this.api.kernel.OptionsManager.getOption("EnableWidescreenPanels") || !this.api.electron.isShowingWidescreenPanel);
                  _loc81_.configureUseFlashChat(_loc82_);
               }
               break;
            case "PING":
               this.api.network.ping();
               break;
            case "MAPID":
               this.showMessage(undefined,"carte : " + this.api.datacenter.Map.id,"DEBUG_LOG");
               this.showMessage(undefined,"Area : " + this.api.datacenter.Map.area,"DEBUG_LOG");
               this.showMessage(undefined,"Sub area : " + this.api.datacenter.Map.subarea,"DEBUG_LOG");
               this.showMessage(undefined,"Super Area : " + this.api.datacenter.Map.superarea,"DEBUG_LOG");
               break;
            case "CELLID":
               this.showMessage(undefined,"cellule : " + this.api.datacenter.Player.data.cellNum,"DEBUG_LOG");
               break;
            case "TIME":
               this.showMessage(undefined,"Heure : " + this.api.kernel.NightManager.time,"DEBUG_LOG");
               break;
            case "CACHE":
               this.api.kernel.askClearCache();
               break;
            case "REBOOT":
               this.api.kernel.reboot();
               break;
            case "FPS":
               this.api.ui.getUIComponent("Debug").showFps();
               break;
            case "CONSOLECLOSE":
               this.api.ui.getUIComponent("Debug").callClose();
               break;
            case "UI":
               this.api.ui.loadUIComponent(_loc3_[0],_loc3_[0]);
               break;
            case "DEBUG":
               dofus.Constants.DEBUG = !dofus.Constants.DEBUG;
               this.showMessage(undefined,"DEBUG : " + dofus.Constants.DEBUG,"DEBUG_LOG");
               break;
            case "ASKOK":
               this.api.ui.loadUIComponent("AskOk","AskOkContent",{title:"AskOKDebug",text:this.api.lang.getText(_loc3_[0],_loc3_.splice(1))});
               break;
            case "ASKOK2":
               _loc83_ = "";
               _loc84_ = 0;
               while(_loc84_ < _loc3_.length)
               {
                  if(_loc84_ > 0)
                  {
                     _loc83_ += " ";
                  }
                  _loc83_ += _loc3_[_loc84_];
                  _loc84_ = _loc84_ + 1;
               }
               this.api.ui.loadUIComponent("AskOk","AskOkContent",{title:"AskOKDebug",text:_loc83_});
               break;
            case "MOVIECLIP":
               this.api.kernel.findMovieClipPath();
               break;
            case "TOGGLESPRITES":
               this.api.datacenter.Basics.gfx_isSpritesHidden = !this.api.datacenter.Basics.gfx_isSpritesHidden;
               if(this.api.datacenter.Basics.gfx_isSpritesHidden)
               {
                  this.api.gfx.spriteHandler.maskAllSprites();
               }
               else
               {
                  this.api.gfx.spriteHandler.unmaskAllSprites();
               }
               break;
            case "DRAWZONE":
               _loc85_ = Number(_loc3_[0]);
               _loc86_ = Number(_loc3_[1]);
               _loc87_ = _loc3_[2];
               _loc88_ = _loc3_[3] == undefined ? undefined : Number(_loc3_[3]);
               if(_global.isNaN(_loc85_) || (_loc85_ == undefined || (_global.isNaN(_loc86_) || (_loc86_ == undefined || (_loc87_ == undefined || _loc87_.length == 0)))))
               {
                  this.showMessage(undefined,"Usage : /drawzone [num cell] [radius] [layer]","DEBUG_LOG");
                  return undefined;
               }
               this.api.gfx.drawZone(_loc85_,0,_loc86_,_loc87_,dofus.Constants.ZONE_COLOR[_loc87_],_loc88_);
               break;
            case "CLEARZONE":
               _loc89_ = Number(_loc3_[0]);
               _loc90_ = Number(_loc3_[1]);
               _loc91_ = _loc3_[2];
               if(_global.isNaN(_loc89_) || (_loc89_ == undefined || (_global.isNaN(_loc90_) || (_loc90_ == undefined || (_loc91_ == undefined || _loc91_.length == 0)))))
               {
                  this.showMessage(undefined,"Usage : /clearzone [num cell] [radius] [layer]","DEBUG_LOG");
                  return undefined;
               }
               this.api.gfx.clearZone(_loc89_,_loc90_,_loc91_);
               break;
            case "CLEANCELLS":
               this.api.gfx.mapHandler.resetEmptyCells();
               break;
            case "SEQACTIONS":
               _loc92_ = this.api.datacenter.Sprites.getItems();
               for(var k in _loc92_)
               {
                  _loc93_ = k;
                  _loc94_ = _loc92_[_loc93_];
                  _loc95_ = _loc94_.sequencer;
                  this.showMessage(undefined,"    Print Sequencer Actions List for " + _loc93_ + ", " + _loc94_.name,"DEBUG_LOG");
                  _loc95_.printActions();
               }
               break;
            case "LOS":
               _loc96_ = Number(_loc3_[0]);
               _loc97_ = Number(_loc3_[1]);
               if(_global.isNaN(_loc96_) || (_loc96_ == undefined || (_global.isNaN(_loc97_) || _loc97_ == undefined)))
               {
                  this.showMessage(undefined,"Unable to resolve case ID","DEBUG_LOG");
                  return undefined;
               }
               this.showMessage(undefined,"Line of sight between " + _loc96_ + " and " + _loc97_ + " -> " + ank.battlefield.utils.Pathfinding.checkView(this.api.gfx.mapHandler,_loc96_,_loc97_),"DEBUG_LOG");
               break;
            case "CLEARCELL":
               _loc98_ = Number(_loc3_[0]);
               if(_global.isNaN(_loc98_) || _loc98_ == undefined)
               {
                  this.showMessage(undefined,"I\'ll need an ID!","DEBUG_LOG");
                  return undefined;
               }
               this.api.gfx.mapHandler.getCellData(_loc98_).removeAllSpritesOnID();
               this.showMessage(undefined,"Cell " + _loc98_ + " cleaned.","DEBUG_LOG");
               break;
            case "CELLINFO":
               _loc99_ = Number(_loc3_[0]);
               if(_global.isNaN(_loc99_) || _loc99_ == undefined)
               {
                  this.showMessage(undefined,"I\'ll need an ID!","DEBUG_LOG");
                  return undefined;
               }
               _loc100_ = this.api.gfx.mapHandler.getCellData(_loc99_);
               this.showMessage(undefined,"Datas about cell " + _loc99_ + ":","DEBUG_LOG");
               for(var k in _loc100_)
               {
                  this.showMessage(undefined,"    " + k + " -> " + _loc100_[k],"DEBUG_LOG");
                  if(_loc100_[k] instanceof Object)
                  {
                     for(var l in _loc100_[k])
                     {
                        this.showMessage(undefined,"        " + l + " -> " + _loc100_[k][l],"DEBUG_LOG");
                     }
                  }
               }
               break;
            case "SOMAPLAY":
               _loc101_ = _loc3_.join(" ");
               this.api.kernel.AudioManager.playSound(_loc101_);
               break;
            case "SKIPFIGHTANIMATIONS":
               if(!this.api.datacenter.Player.isAuthorized)
               {
                  this.showMessage(undefined,"(°~°)","ERROR_BOX");
               }
               return undefined;
            case "SKIPLOOTPANEL":
               if(!this.api.datacenter.Player.isAuthorized)
               {
                  this.showMessage(undefined,"(°~°)","ERROR_BOX");
               }
               return undefined;
            case "SHOWCELLIDS":
               if(_loc3_[0] == "1")
               {
                  this.api.gfx.showCellIds();
               }
               else if(_loc3_[0] == "0")
               {
                  this.api.gfx.hideCellIds();
               }
               else
               {
                  !this.api.gfx.showingCellIds ? this.api.gfx.showCellIds() : this.api.gfx.hideCellIds();
               }
               if(this.api.gfx.showingCellIds && this.api.kernel.OptionsManager.getOption("Grid") == false)
               {
                  this.api.kernel.OptionsManager.setOption("Grid",true);
               }
               break;
            default:
               this.showMessage(undefined,this.api.lang.getText("UNKNOW_COMMAND",[_loc4_]),"DEBUG_ERROR");
         }
      }
      else if(this.api.datacenter.Basics.isLogged)
      {
         sCmd = this.analyseCmd(sCmd);
         this.api.network.Basics.autorisedCommand(sCmd);
      }
      else
      {
         this.showMessage(undefined,this.api.lang.getText("UNKNOW_COMMAND",[sCmd]),"DEBUG_ERROR");
      }
   }
   function analyseCmd(sCmd)
   {
      var _loc3_ = sCmd.split(" ");
      var _loc4_ = _loc3_.length <= 0 ? "" : String(_loc3_[0]).toLowerCase();
      var _loc0_;
      if((_loc0_ = _loc4_) === "hash")
      {
         if(_loc3_.length > 1 && (_loc3_.length < 3 && String(_loc3_[1]).toUpperCase() == "ADD"))
         {
            sCmd = "";
            this.api.electron.getHash();
         }
      }
      return sCmd;
   }
   function showMessage(sTitle, sMsg, sType)
   {
      var _loc5_ = dofus.graphics.gapi.ui.Debug.FILE_OUTPUT_STATE;
      var _loc6_;
      if(this.api.electron.enabled)
      {
         _loc6_ = new ank.utils.ExtendedString(sMsg).externalInterfaceEscape();
         this.api.electron.consolePrint(_loc6_,sType,undefined,true);
         if(_loc5_ != 0)
         {
            this.api.electron.consoleLog(sType,_loc6_,true);
            if(_loc5_ == 2)
            {
               return undefined;
            }
         }
      }
      if(!this.api.electron.isShowingWidescreenPanel || this.api.electron.getWidescreenPanelId() != dofus.Electron.WIDESCREEN_PANEL_CONSOLE)
      {
         this.api.kernel.showMessage(sTitle,sMsg,sType);
      }
   }
}
