class dofus.utils.consoleParsers.ChatConsoleParser extends dofus.utils.consoleParsers.AbstractConsoleParser
{
   var _aWhisperHistory;
   var _nWhisperHistoryPointer;
   function ChatConsoleParser(oAPI)
   {
      super();
      this.initialize(oAPI);
   }
   function initialize(oAPI)
   {
      super.initialize(oAPI);
      this._aWhisperHistory = [];
      this._nWhisperHistoryPointer = 0;
   }
   function process(sCmd, oParams)
   {
      if(!this.api.datacenter.Basics.inGame)
      {
         this.api.kernel.showMessage(undefined,this.api.lang.getText("SRV_MSG_7"),"ERROR_CHAT");
         return undefined;
      }
      super.process(sCmd,oParams);
      sCmd = this.parseSpecialDatas(sCmd);
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
      if(sCmd.charAt(0) == "/")
      {
         _loc6_ = sCmd.split(" ");
         _loc7_ = _loc6_[0].substr(1).toUpperCase();
         _loc8_ = "/" + _loc7_.toLowerCase();
         _loc6_.splice(0,1);
         while(_loc6_[0].length == 0)
         {
            _loc6_.splice(0,1);
         }
         loop7:
         switch(_loc7_)
         {
            case "HELP":
            case "H":
            case "?":
               this.api.kernel.showMessage(undefined,this.api.lang.getText("COMMANDS_HELP"),"COMMANDS_CHAT");
               break;
            case "ROLL":
               _loc9_ = this.api.lang.getText("CHAT_COMMAND_INVALID",[_loc8_]);
               _loc10_ = 1;
               while(_loc10_ <= 3)
               {
                  _loc9_ += "\n- " + this.api.lang.getText("DICE_HELP_" + _loc10_,[_loc8_]);
                  _loc10_ += 1;
               }
               if(_loc6_.length < 1)
               {
                  this.api.kernel.showMessage(undefined,_loc9_,"COMMANDS_CHAT");
                  break;
               }
               _loc11_ = _loc6_[0];
               if(_loc11_.length < 1)
               {
                  this.api.kernel.showMessage(undefined,_loc9_,"COMMANDS_CHAT");
                  break;
               }
               _loc12_ = false;
               if(_loc11_.charAt(0).toLowerCase() == "g")
               {
                  _loc12_ = true;
                  _loc11_ = _loc11_.substring(1);
               }
               if(_loc11_.indexOf("d") > -1)
               {
                  _loc13_ = _loc11_.split("d");
                  _loc14_ = Number(_loc13_[0]);
                  _loc15_ = Number(_loc13_[1]);
               }
               else
               {
                  _loc14_ = 1;
                  _loc15_ = Number(_loc11_);
               }
               if(_global.isNaN(_loc14_))
               {
                  this.api.kernel.showMessage(undefined,_loc9_,"COMMANDS_CHAT");
                  break;
               }
               if(_global.isNaN(_loc15_))
               {
                  this.api.kernel.showMessage(undefined,_loc9_,"COMMANDS_CHAT");
                  break;
               }
               this.api.network.Evenemential.sendRollDice(_loc14_,_loc15_,!_loc12_ ? "*" : "%");
               break;
            case "VERSION":
            case "VER":
            case "ABOUT":
               _loc16_ = "--------------------------------------------------------------\n";
               _loc16_ += "<b>DOFUS RETRO Client v" + dofus.Constants.VERSION + "." + dofus.Constants.SUBVERSION + "." + dofus.Constants.SUBSUBVERSION + "</b>";
               if(dofus.Constants.BETAVERSION > 0)
               {
                  _loc16_ += " <b><font color=\"#FF0000\">BETA VERSION " + dofus.Constants.BETAVERSION + "</font></b>";
               }
               _loc16_ += "\n(c) ANKAMA GAMES (" + dofus.Constants.VERSIONDATE + ")\n";
               _loc16_ += "Flash player " + System.capabilities.version + "\n";
               _loc16_ += "--------------------------------------------------------------";
               this.api.kernel.showMessage(undefined,_loc16_,"COMMANDS_CHAT");
               break;
            case "S":
               this.api.network.Chat.send(_loc6_.join(" "),"*",oParams);
               break;
            case "T":
               this.api.network.Chat.send(_loc6_.join(" "),"#",oParams);
               break;
            case "G":
               if(this.api.datacenter.Player.guildInfos != undefined)
               {
                  this.api.network.Chat.send(_loc6_.join(" "),"%",oParams);
               }
               break;
            case "P":
               if(this.api.ui.getUIComponent("Party") != undefined)
               {
                  this.api.network.Chat.send(_loc6_.join(" "),"$",oParams);
               }
               break;
            case "A":
               this.api.network.Chat.send(_loc6_.join(" "),"!",oParams);
               break;
            case "R":
               this.api.network.Chat.send(_loc6_.join(" "),"?",oParams);
               break;
            case "B":
               this.api.network.Chat.send(_loc6_.join(" "),":",oParams);
               break;
            case "I":
               this.api.network.Chat.send(_loc6_.join(" "),"^",oParams);
               break;
            case "Q":
               this.api.network.Chat.send(_loc6_.join(" "),"@",oParams);
               break;
            case "M":
               this.api.network.Chat.send(_loc6_.join(" "),"¤",oParams);
               break;
            case "W":
            case "MSG":
            case "WHISPER":
               if(_loc6_.length < 2)
               {
                  this.api.kernel.showMessage(undefined,this.api.lang.getText("SYNTAX_ERROR",[" /w &lt;" + this.api.lang.getText("NAME") + "&gt; &lt;" + this.api.lang.getText("MSG") + "&gt;"]),"ERROR_CHAT");
                  break;
               }
               _loc17_ = _loc6_[0];
               if(_loc17_.length < 2)
               {
                  this.api.kernel.showMessage(undefined,this.api.lang.getText("SYNTAX_ERROR",[" /w &lt;" + this.api.lang.getText("NAME") + "&gt; &lt;" + this.api.lang.getText("MSG") + "&gt;"]),"ERROR_CHAT");
                  break;
               }
               _loc6_.shift();
               _loc18_ = _loc6_.join(" ");
               this.pushWhisper("/w " + _loc17_ + " ");
               this.api.network.Chat.send(_loc18_,_loc17_,oParams);
               break;
            case "WHOAMI":
               this.api.network.Basics.whoAmI();
               break;
            case "WHOIS":
               if(_loc6_.length == 0)
               {
                  this.api.kernel.showMessage(undefined,this.api.lang.getText("SYNTAX_ERROR",[" /whois &lt;" + this.api.lang.getText("NAME") + "&gt;"]),"ERROR_CHAT");
                  break;
               }
               this.api.network.Basics.whoIs(_loc6_[0]);
               break;
            case "F":
            case "FRIEND":
            case "FRIENDS":
               switch(_loc6_[0].toUpperCase())
               {
                  case "A":
                  case "+":
                     this.api.network.Friends.addFriend(_loc6_[1]);
                     break;
                  case "D":
                  case "R":
                  case "-":
                     this.api.network.Friends.removeFriend(_loc6_[1]);
                     break;
                  case "L":
                     this.api.network.Friends.getFriendsList();
                     break;
                  default:
                     this.api.kernel.showMessage(undefined,this.api.lang.getText("SYNTAX_ERROR",[" /f &lt;A/D/L&gt; &lt;" + this.api.lang.getText("NAME") + "&gt;"]),"ERROR_CHAT");
               }
               break;
            case "IGNORE":
            case "ENEMY":
               switch(_loc6_[0].toUpperCase())
               {
                  case "A":
                  case "+":
                     this.api.network.Enemies.addEnemy(_loc6_[1]);
                     break;
                  case "D":
                  case "R":
                  case "-":
                     this.api.network.Enemies.removeEnemy(_loc6_[1]);
                     break;
                  case "L":
                     this.api.network.Enemies.getEnemiesList();
                     break;
                  default:
                     this.api.kernel.showMessage(undefined,this.api.lang.getText("SYNTAX_ERROR",[" /f &lt;A/D/L&gt; &lt;" + this.api.lang.getText("NAME") + "&gt;"]),"ERROR_CHAT");
               }
               break;
            case "PING":
               this.api.network.ping();
               break;
            case "GOD":
            case "GODMODE":
               _loc19_ = Math.random();
               _loc20_ = [];
               _loc21_ = "Retro Legacy";
               _loc22_ = "Hall des Valeureux du Dieu Iop";
               _loc23_ = "Retro 1.30+";
               _loc24_ = ["Bill","Tyn","Nyx","Lichen","Simsoft"];
               _loc25_ = ["Sastip","Papinaut","Iotam"];
               _loc26_ = ["Kam","ToT","LeLag","Sannho","Treuff","Artand","Ekyn","Simeth","Asthenis","Oopah","Seydlex","Eknelis"];
               _loc27_ = 0;
               while(_loc27_ < _loc24_.length)
               {
                  _loc20_.push({pseudo:_loc24_[_loc27_],godtype:_loc21_});
                  _loc27_ += 1;
               }
               _loc28_ = 0;
               while(_loc28_ < _loc25_.length)
               {
                  _loc20_.push({pseudo:_loc25_[_loc28_],godtype:_loc22_});
                  _loc28_ += 1;
               }
               _loc29_ = 0;
               while(_loc29_ < _loc26_.length)
               {
                  _loc20_.push({pseudo:_loc26_[_loc29_],godtype:_loc23_});
                  _loc29_ += 1;
               }
               _loc20_.push({pseudo:"DUSK",godtype:"Retro & Retro Remastered 1.30+, Swiss Made"});
               _loc20_.push({pseudo:"Lakha",godtype:"Retro 1.30+, Détentrice du cahier de l\'annulation suprême"});
               _loc20_.push({pseudo:"Logan",godtype:"Retro 1.30+, Ch\'pécialiste de la divulgach\'"});
               _loc30_ = _loc20_[Math.floor(Math.random() * _loc20_.length)];
               this.api.kernel.showMessage(undefined,"God : <u>" + _loc30_.pseudo + "</u> (<b>" + _loc30_.godtype + "</b>)","COMMANDS_CHAT");
               break;
            case "APING":
               this.api.kernel.showMessage(undefined,"Average ping : " + this.api.network.getAveragePing() + "ms (on " + this.api.network.getAveragePingPacketsCount() + " packets)","COMMANDS_CHAT");
               break;
            case "MAPID":
               this.api.kernel.showMessage(undefined,"MAP ID : " + this.api.datacenter.Map.id,"COMMANDS_CHAT");
               if(this.api.datacenter.Player.isAuthorized)
               {
                  this.api.kernel.showMessage(undefined,"Area : " + this.api.datacenter.Map.area,"COMMANDS_CHAT");
                  this.api.kernel.showMessage(undefined,"Sub area : " + this.api.datacenter.Map.subarea,"COMMANDS_CHAT");
                  this.api.kernel.showMessage(undefined,"Super Area : " + this.api.datacenter.Map.superarea,"COMMANDS_CHAT");
               }
               break;
            case "CELLID":
               this.api.kernel.showMessage(undefined,"CELL ID : " + this.api.datacenter.Player.data.cellNum,"COMMANDS_CHAT");
               break;
            case "TIME":
               this.api.kernel.showMessage(undefined,this.api.kernel.NightManager.date + " - " + this.api.kernel.NightManager.time,"COMMANDS_CHAT");
               break;
            case "LIST":
            case "PLAYERS":
               if(!this.api.datacenter.Game.isFight)
               {
                  this.api.kernel.showMessage(undefined,this.api.lang.getText("CANT_DO_COMMAND_HERE",[_loc7_]),"ERROR_CHAT");
                  return undefined;
               }
               _loc31_ = [];
               _loc32_ = this.api.datacenter.Sprites.getItems();
               for(var _loc33_ in _loc32_)
               {
                  if(_loc32_[_loc33_] instanceof dofus.datacenter.Character)
                  {
                     _loc31_.push("- " + _loc32_[_loc33_].name);
                  }
               }
               this.api.kernel.showMessage(undefined,this.api.lang.getText("PLAYERS_LIST") + " :\n" + _loc31_.join("\n"),"COMMANDS_CHAT");
               break;
            case "KICK":
               if(!this.api.datacenter.Game.isFight || this.api.datacenter.Game.isRunning)
               {
                  this.api.kernel.showMessage(undefined,this.api.lang.getText("CANT_DO_COMMAND_HERE",[_loc7_]),"ERROR_CHAT");
                  return undefined;
               }
               _loc34_ = String(_loc6_[0]);
               _loc35_ = this.api.datacenter.Sprites.getItems();
               for(_loc33_ in _loc35_)
               {
                  if(_loc35_[_loc33_] instanceof dofus.datacenter.Character && _loc35_[_loc33_].name == _loc34_)
                  {
                     _loc36_ = _loc35_[_loc33_].id;
                     break loop7;
                  }
               }
               if(_loc36_ != undefined)
               {
                  this.api.network.Game.leave(_loc36_);
               }
               else
               {
                  this.api.kernel.showMessage(undefined,this.api.lang.getText("CANT_KICK_A",[_loc34_]),"ERROR_CHAT");
               }
               break;
            case "SPECTATOR":
            case "SPEC":
               if(!this.api.datacenter.Game.isRunning || this.api.datacenter.Game.isSpectator)
               {
                  this.api.kernel.showMessage(undefined,this.api.lang.getText("CANT_DO_COMMAND_HERE",[_loc7_]),"ERROR_CHAT");
                  return undefined;
               }
               this.api.network.Fights.blockSpectators();
               break;
            case "AWAY":
               this.api.network.Basics.away();
               break;
            case "INVISIBLE":
               this.api.network.Basics.invisible();
               break;
            case "INVITE":
               _loc37_ = String(_loc6_[0]);
               if(_loc37_.length == 0 || _loc37_ == undefined)
               {
                  break;
               }
               this.api.network.Party.invite(_loc37_);
               break;
            case "CONSOLE":
               if(this.api.datacenter.Player.isAuthorized)
               {
                  this.api.ui.loadUIComponent("Debug","Debug",undefined,{bAlwaysOnTop:true});
               }
               else
               {
                  this.api.kernel.showMessage(undefined,this.api.lang.getText("UNKNOW_COMMAND",[_loc7_]),"ERROR_CHAT");
               }
               break;
            case "DEBUG":
               if(this.api.datacenter.Player.isAuthorized)
               {
                  this.api.kernel.DebugManager.toggleDebug();
               }
               break;
            case "CHANGECHARACTER":
               this.api.kernel.changeServer();
               break;
            case "LOGOUT":
               this.api.kernel.disconnect();
               break;
            case "QUIT":
               this.api.kernel.quit();
               break;
            case "THINK":
            case "METHINK":
            case "PENSE":
            case "TH":
               if(_loc6_.length < 1)
               {
                  this.api.kernel.showMessage(undefined,this.api.lang.getText("SYNTAX_ERROR",[" /" + _loc7_.toLowerCase() + " &lt;" + this.api.lang.getText("TEXT_WORD") + "&gt;"]),"ERROR_CHAT");
                  break;
               }
               _loc38_ = "!THINK!" + _loc6_.join(" ");
               if(this.api.datacenter.Player.canChatToAll)
               {
                  this.api.network.Chat.send(_loc38_,"*",oParams);
               }
               break;
            case "ME":
            case "EM":
            case "MOI":
            case "EMOTE":
               if(!this.api.lang.getConfigText("EMOTES_ENABLED"))
               {
                  this.api.kernel.showMessage(undefined,this.api.lang.getText("UNKNOW_COMMAND",[_loc7_]),"ERROR_CHAT");
                  break;
               }
               if(_loc6_.length < 1)
               {
                  this.api.kernel.showMessage(undefined,this.api.lang.getText("SYNTAX_ERROR",[" /" + _loc7_.toLowerCase() + " &lt;" + this.api.lang.getText("TEXT_WORD") + "&gt;"]),"ERROR_CHAT");
                  break;
               }
               _loc39_ = _loc6_.join(" ");
               if(this.api.datacenter.Player.canChatToAll)
               {
                  this.api.network.Chat.send(dofus.Constants.EMOTE_CHAR + _loc39_ + dofus.Constants.EMOTE_CHAR,"*",oParams);
               }
               break;
            case "KB":
               this.api.ui.loadUIComponent("KnownledgeBase","KnownledgeBase");
               break;
            case "RELEASE":
               if(this.api.datacenter.Player.data.isTomb)
               {
                  this.api.network.Game.freeMySoul();
               }
               else if(this.api.datacenter.Player.data.isSlow)
               {
                  this.api.kernel.showMessage(undefined,this.api.lang.getText("ERROR_ALREADY_A_GHOST"),"ERROR_CHAT");
               }
               else
               {
                  this.api.kernel.showMessage(undefined,this.api.lang.getText("ERROR_NOT_DEAD_AT_LEAST_FOR_NOW"),"ERROR_CHAT");
               }
               break;
            case "SELECTION":
               if(_loc6_[0] == "enable" || _loc6_[0] == "on")
               {
                  dofus.graphics.gapi.ui.Banner(this.api.ui.getUIComponent("Banner")).setSelectable(true);
               }
               else if(_loc6_[0] == "disable" || _loc6_[0] == "off")
               {
                  dofus.graphics.gapi.ui.Banner(this.api.ui.getUIComponent("Banner")).setSelectable(false);
               }
               else
               {
                  this.api.kernel.showMessage(undefined,this.api.lang.getText("SYNTAX_ERROR",["/selection [enable|on|disable|off]"]),"ERROR_CHAT");
               }
               break;
            case "AUTOSCROLL":
               if(_loc6_[0] == "enable" || _loc6_[0] == "on")
               {
                  this.api.kernel.showMessage(undefined,"Autoscroll du chat réactivé","INFO_CHAT");
                  dofus.graphics.gapi.ui.Banner(this.api.ui.getUIComponent("Banner")).setChatAutoScroll(true);
               }
               else if(_loc6_[0] == "disable" || _loc6_[0] == "off")
               {
                  this.api.kernel.showMessage(undefined,"Autoscroll du chat désactivé","INFO_CHAT");
                  dofus.graphics.gapi.ui.Banner(this.api.ui.getUIComponent("Banner")).setChatAutoScroll(false);
               }
               else
               {
                  this.api.kernel.showMessage(undefined,this.api.lang.getText("SYNTAX_ERROR",["/autoscroll [enable|on|disable|off]"]),"ERROR_CHAT");
               }
               break;
            case "WTF":
            case "DOFUS2":
               this.api.kernel.showMessage(undefined,"(°~°)","ERROR_BOX");
               break;
            case "TACTIC":
               if(this.api.datacenter.Player.isAuthorized || this.api.datacenter.Game.isFight)
               {
                  _loc40_ = !this.api.datacenter.Game.isTacticMode;
                  this.api.datacenter.Game.isTacticMode = _loc40_;
                  this.api.gfx.activateTacticMode(this.api,_loc40_);
                  this.api.ui.getUIComponent("FightOptionButtons")._btnTactic.selected = _loc40_;
               }
               break;
            case "RETROCHAT":
            case "CHATOUTPUT":
               if(!this.api.electron.enabled)
               {
                  this.api.kernel.showMessage(undefined,"Does not work on a Flash Projector","ERROR_CHAT");
                  break;
               }
               dofus.Electron.retroChatOpen();
               break;
            case "FILEOUTPUT":
               if(this.api.electron.enabled)
               {
                  _loc41_ = Number(_loc6_[0]);
                  if(_loc6_[0] == undefined || (_global.isNaN(_loc41_) || (_loc41_ < 0 || _loc41_ > 2)))
                  {
                     this.api.kernel.showMessage(undefined,"/fileoutput &lt;0 (disabled) | 1 (enabled) | 2 (full)&gt;","ERROR_CHAT");
                     return undefined;
                  }
                  _loc42_ = "";
                  switch(_loc41_)
                  {
                     case 0:
                        _loc42_ = "Disabled";
                        break;
                     case 1:
                        _loc42_ = "Enabled";
                        break;
                     case 2:
                        _loc42_ = "Enabled (full)";
                  }
                  this.api.kernel.ChatManager.fileOutput = _loc41_;
                  this.api.kernel.showMessage(undefined,"File Output (Chat) : " + _loc42_,"COMMANDS_CHAT");
               }
               else
               {
                  this.api.kernel.showMessage(undefined,"Does not work on a Flash Projector","COMMANDS_CHAT");
               }
               break;
            case "CLS":
            case "CLEAR":
               this.api.electron.retroChatClear();
               this.api.kernel.ChatManager.clear();
               this.api.kernel.ChatManager.refresh(true);
               break;
            case "SPEAKINGITEM":
               if(this.api.datacenter.Player.isAuthorized)
               {
                  this.api.kernel.showMessage(undefined,"Count : " + this.api.kernel.SpeakingItemsManager.nextMsgDelay,"ERROR_CHAT");
                  break;
               }
            default:
               _loc43_ = this.api.lang.getEmoteID(_loc7_.toLowerCase());
               if(_loc43_ != undefined)
               {
                  this.api.network.Emotes.useEmote(_loc43_);
                  break;
               }
               this.api.kernel.showMessage(undefined,this.api.lang.getText("UNKNOW_COMMAND",[_loc7_]),"ERROR_CHAT");
         }
      }
      else if(this.api.datacenter.Player.canChatToAll)
      {
         this.api.network.Chat.send(sCmd,"*",oParams);
      }
   }
   function pushWhisper(sCmd)
   {
      var _loc3_ = this._aWhisperHistory.slice(-1);
      var _loc4_;
      if(_loc3_[0] != sCmd)
      {
         _loc4_ = this._aWhisperHistory.push(sCmd);
         if(_loc4_ > 50)
         {
            this._aWhisperHistory.shift();
         }
      }
      this.initializePointers();
   }
   function getWhisperHistoryUp()
   {
      if(this._nWhisperHistoryPointer > 0)
      {
         this._nWhisperHistoryPointer--;
      }
      var _loc2_ = this._aWhisperHistory[this._nWhisperHistoryPointer];
      _loc2_ = _loc2_ == undefined ? "" : _loc2_;
      return _loc2_;
   }
   function getWhisperHistoryDown()
   {
      if(this._nWhisperHistoryPointer < this._aWhisperHistory.length)
      {
         this._nWhisperHistoryPointer++;
      }
      var _loc2_ = this._aWhisperHistory[this._nWhisperHistoryPointer];
      _loc2_ = _loc2_ == undefined ? "" : _loc2_;
      return _loc2_;
   }
   function getCurrentPercent()
   {
      var _loc2_ = this.api.datacenter.Player.XPhigh - this.api.datacenter.Player.XPlow;
      if(_loc2_ <= 0)
      {
         return "100%";
      }
      var _loc3_ = Math.floor((this.api.datacenter.Player.XP - this.api.datacenter.Player.XPlow) / _loc2_ * 100) + "%";
      return _loc3_;
   }
   function initializePointers()
   {
      super.initializePointers();
      this._nWhisperHistoryPointer = this._aWhisperHistory.length;
   }
   function parseSpecialDatas(s)
   {
      ank.utils.Extensions.addExtensions();
      var _loc3_ = this.api.lang.getText("INLINE_VARIABLE_POSITION").split(",");
      s = new ank.utils.ExtendedString(s).replace(_loc3_,"[" + this.api.datacenter.Map.x + ", " + this.api.datacenter.Map.y + "]");
      var _loc4_ = this.api.lang.getText("INLINE_VARIABLE_AREA").split(",");
      s = new ank.utils.ExtendedString(s).replace(_loc4_,this.api.lang.getMapAreaText(this.api.datacenter.Map.area).n);
      var _loc5_ = this.api.lang.getText("INLINE_VARIABLE_SUBAREA").split(",");
      s = new ank.utils.ExtendedString(s).replace(_loc5_,this.api.lang.getMapSubAreaText(this.api.datacenter.Map.subarea).n);
      var _loc6_ = this.api.lang.getText("INLINE_VARIABLE_MYSELF").split(",");
      s = new ank.utils.ExtendedString(s).replace(_loc6_,this.api.datacenter.Player.Name);
      var _loc7_ = this.api.lang.getText("INLINE_VARIABLE_LEVEL").split(",");
      s = new ank.utils.ExtendedString(s).replace(_loc7_,String(this.api.datacenter.Player.Level));
      var _loc8_ = this.api.lang.getText("INLINE_VARIABLE_GUILD").split(",");
      var _loc9_ = this.api.datacenter.Player.guildInfos.name;
      if(_loc9_ == undefined)
      {
         _loc9_ = this.api.lang.getText("INLINE_VARIABLE_GUILD_ERROR");
      }
      s = new ank.utils.ExtendedString(s).replace(_loc8_,_loc9_);
      var _loc10_ = this.api.lang.getText("INLINE_VARIABLE_MAXLIFE").split(",");
      s = new ank.utils.ExtendedString(s).replace(_loc10_,String(this.api.datacenter.Player.LPmax));
      var _loc11_ = this.api.lang.getText("INLINE_VARIABLE_LIFE").split(",");
      s = new ank.utils.ExtendedString(s).replace(_loc11_,String(this.api.datacenter.Player.LP));
      var _loc12_ = this.api.lang.getText("INLINE_VARIABLE_LIFEPERCENT").split(",");
      s = new ank.utils.ExtendedString(s).replace(_loc12_,String(Math.round(this.api.datacenter.Player.LP / this.api.datacenter.Player.LPmax * 100)));
      var _loc13_ = this.api.lang.getText("INLINE_VARIABLE_EXPERIENCE").split(",");
      s = new ank.utils.ExtendedString(s).replace(_loc13_,this.getCurrentPercent());
      var _loc14_ = this.api.lang.getText("INLINE_VARIABLE_STATS").split(",");
      var _loc15_;
      if(new ank.utils.ExtendedString(s).replace(_loc14_,"X").length != s.length)
      {
         _loc15_ = this.api.lang.getText("INLINE_VARIABLE_STATS_RESULT",[String(this.api.datacenter.Player.Vitality) + (this.api.datacenter.Player.VitalityXtra == 0 ? "" : " (" + ((this.api.datacenter.Player.VitalityXtra <= 0 ? "" : "+") + String(this.api.datacenter.Player.VitalityXtra)) + ")"),String(this.api.datacenter.Player.Wisdom) + (this.api.datacenter.Player.WisdomXtra == 0 ? "" : " (" + ((this.api.datacenter.Player.WisdomXtra <= 0 ? "" : "+") + String(this.api.datacenter.Player.WisdomXtra)) + ")"),String(this.api.datacenter.Player.Force) + (this.api.datacenter.Player.ForceXtra == 0 ? "" : " (" + ((this.api.datacenter.Player.ForceXtra <= 0 ? "" : "+") + String(this.api.datacenter.Player.ForceXtra)) + ")"),String(this.api.datacenter.Player.Intelligence) + (this.api.datacenter.Player.IntelligenceXtra == 0 ? "" : " (" + ((this.api.datacenter.Player.IntelligenceXtra <= 0 ? "" : "+") + String(this.api.datacenter.Player.IntelligenceXtra)) + ")"),String(this.api.datacenter.Player.Chance) + (this
         .api.datacenter.Player.ChanceXtra == 0 ? "" : " (" + ((this.api.datacenter.Player.ChanceXtra <= 0 ? "" : "+") + String(this.api.datacenter.Player.ChanceXtra)) + ")"),String(this.api.datacenter.Player.Agility) + (this.api.datacenter.Player.AgilityXtra == 0 ? "" : " (" + ((this.api.datacenter.Player.AgilityXtra <= 0 ? "" : "+") + String(this.api.datacenter.Player.AgilityXtra)) + ")"),String(this.api.datacenter.Player.Initiative),String(this.api.datacenter.Player.AP),String(this.api.datacenter.Player.MP)]);
         s = new ank.utils.ExtendedString(s).replace(_loc14_,_loc15_);
      }
      return s;
   }
}
