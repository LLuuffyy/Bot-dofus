class dofus.aks.Chat extends dofus.aks.Handler
{
   var aks;
   var api;
   function Chat(oAKS, oAPI)
   {
      super.initialize(oAKS,oAPI);
   }
   function send(sMessage, sDest, oParams)
   {
      if(this.api.datacenter.Game.isSpectator && sDest == "*")
      {
         sDest = "#";
      }
      if(sDest.toLowerCase() == this.api.datacenter.Player.Name.toLowerCase())
      {
         this.api.kernel.showMessage(undefined,this.api.lang.getText("CANT_WISP_YOURSELF"),"ERROR_CHAT");
         return undefined;
      }
      if(this.api.kernel.ChatManager.isBlacklisted(sDest))
      {
         this.api.kernel.showMessage(undefined,this.api.lang.getText("CANT_WISP_BLACKLISTED"),"ERROR_CHAT");
         return undefined;
      }
      sMessage = new ank.utils.ExtendedString(sMessage).replace(["|"],[""]);
      var _loc5_ = this.api.kernel.ChatManager.applyOutputCensorship(sMessage);
      if(!_loc5_)
      {
         return undefined;
      }
      if(this.api.datacenter.Player.zaapToken == undefined && (sMessage.indexOf(this.api.datacenter.Player.login) > -1 || sMessage.indexOf(this.api.datacenter.Player.password) > -1))
      {
         if(sMessage != undefined && (this.api.datacenter.Player.login != undefined && this.api.datacenter.Player.password != undefined))
         {
            this.api.kernel.showMessage(undefined,this.api.lang.getText("CANT_SAY_YOUR_PASSWORD"),"ERROR_CHAT");
            return undefined;
         }
      }
      if(sMessage.length == 0)
      {
         return undefined;
      }
      var _loc6_ = new String();
      var _loc7_ = oParams.items;
      var _loc8_;
      var _loc9_;
      var _loc10_;
      var _loc11_;
      var _loc12_;
      var _loc13_;
      var _loc14_;
      var _loc15_;
      if(_loc7_.length > 0)
      {
         _loc8_ = 0;
         _loc9_ = 0;
         while(_loc9_ < _loc7_.length)
         {
            _loc10_ = _loc7_[_loc9_];
            _loc11_ = "[" + _loc10_.name + "]";
            _loc12_ = sMessage.indexOf(_loc11_);
            if(_loc12_ != -1)
            {
               _loc13_ = "°" + _loc8_;
               _loc8_ += 1;
               _loc14_ = sMessage.split("");
               _loc14_.splice(_loc12_,_loc11_.length,_loc13_);
               sMessage = _loc14_.join("");
               if(_loc6_.length > 0)
               {
                  _loc6_ += "!";
               }
               _loc15_ = _loc10_.compressedEffects;
               _loc6_ += _loc10_.unicID + "!" + (_loc15_ == undefined ? "." : _loc15_);
            }
            _loc9_ += 1;
         }
      }
      var _loc16_ = _loc6_;
      if(_loc16_.length > dofus.Constants.MAX_DATA_LENGTH)
      {
         _loc16_ = _loc16_.substring(0,dofus.Constants.MAX_DATA_LENGTH - 1);
      }
      if(sMessage.length > dofus.Constants.MAX_MESSAGE_LENGTH && !(dofus.Constants.ALPHA && this.api.datacenter.Player.isAuthorized))
      {
         sMessage = sMessage.substring(0,dofus.Constants.MAX_MESSAGE_LENGTH);
      }
      this.aks.send("BM" + sDest + "|" + sMessage + "|" + _loc16_,true,undefined,true);
   }
   function reportMessage(sCharacterID, sMessageUniqId, sMessage, nReason)
   {
      this.aks.send("BR" + sCharacterID + "|" + sMessage + "|" + sMessageUniqId + "|" + nReason,false);
   }
   function subscribeChannels(nChannel, bSubscribe)
   {
      if(!this.api.datacenter.Basics.inGame)
      {
         this.api.kernel.showMessage(undefined,this.api.lang.getText("SRV_MSG_7"),"ERROR_CHAT");
         return undefined;
      }
      var _loc4_ = "";
      switch(nChannel)
      {
         case 0:
            _loc4_ = "i";
            break;
         case 2:
            _loc4_ = "*";
            break;
         case 3:
            _loc4_ = "#$p";
            break;
         case 4:
            _loc4_ = "%";
            break;
         case 5:
            _loc4_ = "!";
            break;
         case 6:
            _loc4_ = "?";
            break;
         case 7:
            _loc4_ = ":";
            break;
         case 8:
            _loc4_ = "^";
            break;
         case 10:
            _loc4_ = "e";
      }
      this.aks.send("cC" + (!bSubscribe ? "-" : "+") + _loc4_,true);
   }
   function useSmiley(nSmileyID)
   {
      if(getTimer() - this.api.datacenter.Basics.aks_chat_lastActionTime < dofus.Constants.CLICK_MIN_DELAY)
      {
         return undefined;
      }
      this.api.datacenter.Basics.aks_chat_lastActionTime = getTimer();
      this.aks.send("BS" + nSmileyID,true);
   }
   function onSubscribeChannel(sExtraData)
   {
      var _loc3_ = sExtraData.charAt(0) == "+";
      var _loc4_ = sExtraData.substr(1).split("");
      var _loc5_ = 0;
      var _loc6_;
      for(; _loc5_ < _loc4_.length; _loc5_ += 1)
      {
         _loc6_ = 0;
         switch(_loc4_[_loc5_])
         {
            case "i":
               _loc6_ = 0;
               break;
            case "*":
               _loc6_ = 2;
               break;
            case "#":
               _loc6_ = 3;
               break;
            case "$":
               _loc6_ = 3;
               break;
            case "p":
               _loc6_ = 3;
               break;
            case "%":
               _loc6_ = 4;
               break;
            case "!":
               _loc6_ = 5;
               break;
            case "?":
               _loc6_ = 6;
               break;
            case ":":
               _loc6_ = 7;
               break;
            case "^":
               _loc6_ = 8;
               break;
            case "@":
               _loc6_ = 9;
               break;
            case "e":
               _loc6_ = 10;
               break;
            default:
               continue;
         }
         this.api.ui.getUIComponent("Banner").chat.selectFilter(_loc6_,_loc3_);
         this.api.kernel.ChatManager.setTypeVisible(_loc6_,_loc3_);
         this.api.datacenter.Basics.chat_type_visible[_loc6_] = _loc3_;
      }
   }
   function onMessage(bSuccess, sExtraData)
   {
      if(!bSuccess)
      {
         switch(sExtraData.charAt(0))
         {
            case "S":
               this.api.kernel.showMessage(undefined,this.api.lang.getText("SYNTAX_ERROR",[" /w <" + this.api.lang.getText("NAME") + "> <" + this.api.lang.getText("MSG") + ">"]),"ERROR_CHAT");
               break;
            case "f":
               this.api.kernel.showMessage(undefined,this.api.lang.getText("USER_NOT_CONNECTED",[sExtraData.substr(1)]),"ERROR_CHAT");
               break;
            case "e":
               this.api.kernel.showMessage(undefined,this.api.lang.getText("USER_NOT_CONNECTED_BUT_TRY_SEND_EXTERNAL",[sExtraData.substr(1)]),"ERROR_CHAT");
               break;
            case "n":
               this.api.kernel.showMessage(undefined,this.api.lang.getText("USER_NOT_CONNECTED_EXTERNAL_NACK",[sExtraData.substr(1)]),"ERROR_CHAT");
         }
         return undefined;
      }
      var _loc5_ = sExtraData.charAt(0);
      sExtraData = _loc5_ != "|" ? sExtraData.substr(2) : sExtraData.substr(1);
      var _loc6_ = sExtraData.split("|");
      var _loc7_ = _loc6_[2];
      var _loc8_ = _loc6_[1];
      var _loc9_ = _loc6_[0];
      var _loc10_ = _loc6_[3];
      if(this.api.kernel.ChatManager.isBlacklisted(_loc8_))
      {
         return undefined;
      }
      var _loc11_ = _loc7_;
      var _loc12_;
      if(_loc5_ != "e")
      {
         if(_loc10_.length > 0)
         {
            _loc12_ = _loc10_.split("!");
            _loc7_ = this.api.kernel.ChatManager.parseInlineItems(_loc7_,_loc12_,true);
            _loc11_ = this.api.kernel.ChatManager.parseInlineItems(_loc11_,_loc12_,false);
         }
         _loc7_ = this.api.kernel.ChatManager.parseInlinePos(_loc7_);
      }
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
      switch(_loc5_)
      {
         case "F":
            _loc13_ = "WHISP_CHAT";
            _loc7_ = this.api.kernel.ChatManager.parseSecretsEmotes(_loc7_);
            if(!_loc7_.length)
            {
               return undefined;
            }
            _loc14_ = this.api.lang.getText("FROM") + " " + _loc8_ + " : ";
            this.api.electron.makeNotification(_loc14_ + this.api.kernel.ChatManager.applyInputCensorship(_loc7_));
            _loc7_ = this.api.lang.getText("FROM") + " <i>" + this.getLinkName(_loc9_,_loc8_) + "</i> : " + this.getLinkMessage(_loc9_,_loc8_,_loc14_,_loc11_,_loc7_);
            this.api.kernel.Console.pushWhisper("/w " + _loc8_ + " ");
            break;
         case "T":
            _loc13_ = "WHISP_CHAT";
            _loc15_ = this.api.lang.getText("TO_DESTINATION") + " " + _loc8_ + " : ";
            _loc7_ = this.api.lang.getText("TO_DESTINATION") + " " + this.getLinkName(_loc9_,_loc8_) + " : " + this.getLinkMessage(_loc9_,_loc8_,_loc15_,_loc11_,_loc7_);
            break;
         case "#":
            if(this.api.datacenter.Game.isFight)
            {
               _loc13_ = "WHISP_CHAT";
               _loc16_ = this.isLivingItemMessage(_loc7_);
               if(!_loc16_)
               {
                  if(this.api.datacenter.Game.isSpectator)
                  {
                     _loc17_ = "(" + this.api.lang.getText("SPECTATOR") + ")";
                  }
                  else
                  {
                     _loc17_ = "(" + this.api.lang.getText("TEAM") + ")";
                  }
                  _loc18_ = _loc17_ + " " + _loc8_ + " : ";
                  _loc7_ = _loc17_ + " " + this.getLinkName(_loc9_,_loc8_) + " : " + this.getLinkMessage(_loc9_,_loc8_,_loc18_,_loc11_,_loc7_);
                  break;
               }
               if(_loc16_ && !_global.isNaN(_loc7_.substr(2,_loc7_.length - 4)))
               {
                  _loc7_ = this.parseLivingItemMessage(_loc7_,_loc9_,_loc8_,_loc11_);
                  if(_loc7_ == undefined)
                  {
                     return undefined;
                  }
               }
            }
            break;
         case "%":
            _loc13_ = "GUILD_CHAT_SOUND";
            _loc19_ = "(" + this.api.lang.getText("GUILD") + ") " + _loc8_ + " : ";
            _loc7_ = "(" + this.api.lang.getText("GUILD") + ") " + this.getLinkName(_loc9_,_loc8_) + " : " + this.getLinkMessage(_loc9_,_loc8_,_loc19_,_loc11_,_loc7_);
            break;
         case "$":
            _loc13_ = "PARTY_CHAT";
            _loc20_ = "(" + this.api.lang.getText("PARTY") + ") " + _loc8_ + " : ";
            _loc7_ = "(" + this.api.lang.getText("PARTY") + ") " + this.getLinkName(_loc9_,_loc8_) + " : " + this.getLinkMessage(_loc9_,_loc8_,_loc20_,_loc11_,_loc7_);
            break;
         case "!":
            _loc13_ = "PVP_CHAT";
            _loc21_ = "(" + this.api.lang.getText("ALIGNMENT") + ") " + _loc8_ + " : ";
            _loc7_ = "(" + this.api.lang.getText("ALIGNMENT") + ") " + this.getLinkName(_loc9_,_loc8_) + " : " + this.getLinkMessage(_loc9_,_loc8_,_loc21_,_loc11_,_loc7_);
            break;
         case "?":
            _loc13_ = "RECRUITMENT_CHAT";
            _loc22_ = "(" + this.api.lang.getText("RECRUITMENT") + ") " + _loc8_ + " : ";
            _loc7_ = "(" + this.api.lang.getText("RECRUITMENT") + ") " + this.getLinkName(_loc9_,_loc8_) + " : " + this.getLinkMessage(_loc9_,_loc8_,_loc22_,_loc11_,_loc7_);
            break;
         case ":":
            _loc13_ = "TRADE_CHAT";
            _loc23_ = "(" + this.api.lang.getText("TRADE") + ") " + _loc8_ + " : ";
            _loc7_ = "(" + this.api.lang.getText("TRADE") + ") " + this.getLinkName(_loc9_,_loc8_) + " : " + this.getLinkMessage(_loc9_,_loc8_,_loc23_,_loc11_,_loc7_);
            break;
         case "^":
            _loc13_ = "MEETIC_CHAT";
            _loc24_ = "(Hystoria) " + _loc8_ + " : ";
            _loc7_ = "(Hystoria) " + this.getLinkName(_loc9_,_loc8_) + " : " + this.getLinkMessage(_loc9_,_loc8_,_loc24_,_loc11_,_loc7_);
            break;
         case "e":
            _loc13_ = "GAME_EVENTS_CHAT";
            _loc25_ = _loc11_.split(";");
            _loc7_ = "EVENT_" + String(_loc25_[0]) + "," + _loc25_[1];
            break;
         case "@":
            _loc13_ = "ADMIN_CHAT";
            _loc26_ = "(" + this.api.lang.getText("PRIVATE_CHANNEL") + ") " + _loc8_ + " : ";
            _loc7_ = "(" + this.api.lang.getText("PRIVATE_CHANNEL") + ") " + this.getLinkName(_loc9_,_loc8_) + " : " + this.getLinkMessage(_loc9_,_loc8_,_loc26_,_loc11_,_loc7_);
            break;
         default:
            _loc27_ = this.isLivingItemMessage(_loc7_);
            if(this.api.lang.getConfigText("EMOTES_ENABLED") && (!_loc27_ && (_loc7_.charAt(0) == dofus.Constants.EMOTE_CHAR && _loc7_.charAt(_loc7_.length - 1) == dofus.Constants.EMOTE_CHAR)))
            {
               if(!this.api.datacenter.Game.isRunning && this.api.kernel.ChatManager.isTypeVisible(2))
               {
                  _loc28_ = !(_loc7_.charAt(_loc7_.length - 2) == "." && _loc7_.charAt(_loc7_.length - 3) != ".") ? _loc7_ : _loc7_.substr(0,_loc7_.length - 2) + dofus.Constants.EMOTE_CHAR;
                  _loc28_ = dofus.Constants.EMOTE_CHAR + _loc28_.charAt(1).toUpperCase() + _loc28_.substr(2);
                  this.api.gfx.addSpriteBubble(_loc9_,this.api.kernel.ChatManager.applyInputCensorship(_loc28_));
               }
               _loc13_ = "EMOTE_CHAT";
               _loc7_ = _loc7_.substr(1,_loc7_.length - 2);
               if(!dofus.managers.ChatManager.isPonctuation(_loc7_.charAt(_loc7_.length - 1)))
               {
                  _loc7_ += ".";
               }
               _loc7_ = "<i>" + this.getLinkName(_loc9_,_loc8_) + " " + _loc7_.charAt(0).toLowerCase() + _loc7_.substr(1) + "</i>";
               break;
            }
            if(_loc7_.substr(0,7) == "!THINK!")
            {
               _loc7_ = _loc7_.substr(7);
               if(!this.api.datacenter.Game.isRunning && this.api.kernel.ChatManager.isTypeVisible(2))
               {
                  this.api.gfx.addSpriteBubble(_loc9_,this.api.kernel.ChatManager.applyInputCensorship(_loc7_),ank.battlefield.TextHandler.BUBBLE_TYPE_THINK);
               }
               _loc13_ = "THINK_CHAT";
               _loc29_ = _loc8_ + " " + this.api.lang.getText("THINKS_WORD") + " : ";
               _loc7_ = "<i>" + this.getLinkName(_loc9_,_loc8_) + " " + this.api.lang.getText("THINKS_WORD") + " : " + this.getLinkMessage(_loc9_,_loc8_,_loc29_,_loc11_,_loc7_) + "</i>";
               break;
            }
            if(_loc27_ && !_global.isNaN(_loc7_.substr(2,_loc7_.length - 4)))
            {
               _loc13_ = "MESSAGE_CHAT";
               _loc7_ = this.parseLivingItemMessage(_loc7_,_loc9_,_loc8_,_loc11_);
               break;
            }
            if(!this.api.datacenter.Game.isRunning && this.api.kernel.ChatManager.isTypeVisible(2))
            {
               this.api.gfx.addSpriteBubble(_loc9_,this.api.kernel.ChatManager.applyInputCensorship(_loc7_));
            }
            _loc13_ = "MESSAGE_CHAT";
            _loc30_ = _loc8_ + " : ";
            _loc7_ = this.getLinkName(_loc9_,_loc8_) + " : " + this.getLinkMessage(_loc9_,_loc8_,_loc30_,_loc11_,_loc7_);
            if(this.api.datacenter.Player.isAuthorized)
            {
               _loc31_ = this.api.kernel.DebugManager.getTimestamp();
               this.api.kernel.ChatManager.addRawMessage(this.api.datacenter.Map.id,_loc13_,this.getRawFullMessage(_loc30_,_loc11_),_loc31_);
            }
      }
      this.api.kernel.showMessage(undefined,_loc7_,_loc13_);
   }
   function getRawFullMessage(sPreMessage, sRawMessage)
   {
      return sPreMessage + sRawMessage;
   }
   function getLinkMessage(sPlayerID, sPlayerName, sPreMessage, sRawMessage, sMessage)
   {
      var _loc8_ = this.api.kernel.DebugManager.getTimestamp() + " ";
      sMessage = this.api.kernel.ChatManager.applyInputCensorship(sMessage);
      return "<a href=\"asfunction:onHref,ShowMessagePopupMenu," + sPlayerID + "," + sPlayerName + "," + _global.escape(_loc8_ + sPreMessage + sRawMessage) + "\">" + sMessage + "</a>";
   }
   static function getLinkHighlightSprite(sPlayerID, sLinkName)
   {
      return "<a href=\"asfunction:onHref,highlightSprite," + sPlayerID + "\">" + sLinkName + "</a>";
   }
   static function getLinkHighlightSprites(aSpritesIDs, sLinkName)
   {
      return dofus.aks.Chat.getLinkHighlightSprite(aSpritesIDs.join(","),sLinkName);
   }
   function getLinkName(sPlayerID, sPlayerName, bNoBold)
   {
      if(sPlayerID == undefined)
      {
         sPlayerID = "";
      }
      var _loc4_ = "<b>";
      var _loc5_ = "</b>";
      if(bNoBold)
      {
         _loc4_ = "";
         _loc5_ = "";
      }
      return _loc4_ + "<a href=\"asfunction:onHref,ShowPlayerPopupMenu," + sPlayerID + "," + sPlayerName + "\">" + sPlayerName + "</a>" + _loc5_;
   }
   function onServerMessage(sExtraData)
   {
      if(sExtraData != undefined)
      {
         this.api.kernel.showMessage(undefined,sExtraData,"INFO_CHAT");
      }
   }
   function onSmiley(sExtraData)
   {
      var _loc3_ = sExtraData.split("|");
      var _loc4_ = _loc3_[0];
      var _loc5_ = Number(_loc3_[1]);
      if(!this.api.datacenter.Game.isFight && !this.api.datacenter.Game.isRunning)
      {
         if(_loc4_ != this.api.datacenter.Player.ID && this.api.gfx.spriteHandler.isPlayerSpritesHidden || this.api.kernel.ChatManager.isBlacklisted(this.api.datacenter.Sprites.getItemAt(_loc4_).name))
         {
            return undefined;
         }
      }
      this.api.gfx.addSpriteOverHeadItem(_loc4_,"smiley",dofus.graphics.battlefield.SmileyOverHead,[_loc5_],dofus.Constants.SMILEY_DELAY);
   }
   function isLivingItemMessage(sMessage)
   {
      return sMessage.charAt(0) == dofus.Constants.EMOTE_CHAR && (sMessage.charAt(1) == dofus.Constants.EMOTE_CHAR && (sMessage.charAt(sMessage.length - 1) == dofus.Constants.EMOTE_CHAR && sMessage.charAt(sMessage.length - 2) == dofus.Constants.EMOTE_CHAR));
   }
   function parseLivingItemMessage(sMessage, sFromID, sFromName, sRawMessage)
   {
      if(this.api.kernel.OptionsManager.getOption("UseSpeakingItems"))
      {
      }
      var _loc7_ = _global.parseInt(sMessage.substr(2,sMessage.length - 4));
      var _loc8_ = this.api.lang.getSpeakingItemsText(_loc7_ - Number(sFromID));
      var _loc9_;
      if(_loc8_.m)
      {
         sMessage = _loc8_.m;
         if(!this.api.datacenter.Game.isRunning && this.api.kernel.ChatManager.isTypeVisible(2))
         {
            this.api.gfx.addSpriteBubble(sFromID,this.api.kernel.ChatManager.applyInputCensorship(sMessage));
         }
         _loc9_ = sFromName + " : ";
         return this.getLinkName(sFromID,sFromName,true) + " : " + this.getLinkMessage(sFromID,sFromName,_loc9_,sRawMessage,sMessage);
      }
      return undefined;
   }
}
