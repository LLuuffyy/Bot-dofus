class dofus.aks.Guild extends dofus.aks.Handler
{
   var aks;
   var api;
   function Guild(oAKS, oAPI)
   {
      super.initialize(oAKS,oAPI);
   }
   function create(nBackID, nBackColor, nSymbolID, nSymbolColor, sName)
   {
      this.aks.send("gC" + nBackID + "|" + nBackColor + "|" + nSymbolID + "|" + nSymbolColor + "|" + sName);
   }
   function leave()
   {
      this.aks.send("gV");
   }
   function leaveTaxInterface()
   {
      this.aks.send("gITV",false);
   }
   function invite(sPlayerName)
   {
      this.aks.send("gJR" + sPlayerName);
   }
   function acceptInvitation(nPlayerID)
   {
      this.aks.send("gJK" + nPlayerID);
   }
   function refuseInvitation(nPlayerID)
   {
      this.aks.send("gJE" + nPlayerID,false);
   }
   function getInfosBoostsStats()
   {
      this.aks.send("gW",false);
   }
   function getInfosGeneral()
   {
      this.aks.send("gIG",true);
   }
   function getInfosMembers()
   {
      this.aks.send("gIM",true);
   }
   function getInformationsGuild()
   {
      this.aks.send("gII",true);
   }
   function getInfosBoosts()
   {
      this.aks.send("gIB",true);
   }
   function getInfosTaxCollector()
   {
      this.aks.send("gIT",false);
   }
   function getInfosMountPark()
   {
      this.aks.send("gIF",false);
   }
   function getInfosGuildHouses()
   {
      this.aks.send("gIH",false);
   }
   function bann(sPlayerName)
   {
      this.aks.send("gK" + sPlayerName);
   }
   function changeMemberProfil(oMember)
   {
      this.aks.send("gP" + oMember.id + "|" + oMember.rank + "|" + oMember.percentxp + "|" + oMember.rights.value,true);
   }
   function boostCharacteristic(sCharacteristic)
   {
      var _loc3_ = sCharacteristic;
      switch(_loc3_)
      {
         case "c":
            _loc3_ = "k";
            break;
         case "w":
            _loc3_ = "o";
      }
      this.aks.send("gB" + _loc3_,true);
   }
   function boostSpell(nSpellID)
   {
      this.aks.send("gb" + nSpellID,true);
   }
   function hireTaxCollector()
   {
      this.aks.send("gH");
   }
   function joinTaxCollector(nTaxID, nDefenderID)
   {
      this.aks.send("gTJ" + nTaxID + "|" + nDefenderID,false);
   }
   function leaveTaxCollector(nTaxID, nID)
   {
      this.aks.send("gTV" + nTaxID + (nID == undefined ? "" : "|" + nID),false);
   }
   function removeTaxCollector(nID)
   {
      this.aks.send("gF" + nID,false);
   }
   function teleportToGuildHouse(nHouseID, nInstanceID)
   {
      this.aks.send("gh" + nHouseID + "|" + nInstanceID,false);
   }
   function teleportToGuildFarm(nID)
   {
      this.aks.send("gf" + nID,false);
   }
   function editNote(sNote)
   {
      this.aks.send("gEN" + sNote,false,undefined,true);
   }
   function editInfos(sInfos)
   {
      this.aks.send("gEI" + sInfos,false,undefined,true);
   }
   function editRankName(sRanks)
   {
      this.aks.send("gER" + sRanks,false);
   }
   function onNew()
   {
      this.api.ui.loadUIComponent("CreateGuild","CreateGuild");
   }
   function onCreate(bSuccess, sExtraData)
   {
      if(bSuccess)
      {
         this.api.kernel.showMessage(undefined,this.api.lang.getText("GUILD_CREATED"),"INFO_CHAT");
         this.api.ui.loadUIAutoHideComponent("Guild","Guild",{currentTab:"Members"},{bStayIfPresent:true});
      }
      else
      {
         switch(sExtraData)
         {
            case "an":
               this.api.kernel.showMessage(undefined,this.api.lang.getText("GUILD_CREATE_ALLREADY_USE_NAME"),"ERROR_BOX");
               break;
            case "ae":
               this.api.kernel.showMessage(undefined,this.api.lang.getText("GUILD_CREATE_ALLREADY_USE_EMBLEM"),"ERROR_BOX");
               break;
            case "a":
               this.api.kernel.showMessage(undefined,this.api.lang.getText("GUILD_CREATE_ALLREADY_IN_GUILD"),"ERROR_BOX");
         }
         this.api.ui.getUIComponent("CreateGuild").enabled = true;
      }
   }
   function onStats(sExtraData)
   {
      var _loc4_ = sExtraData.split("|");
      var _loc5_ = _loc4_[0];
      var _loc6_ = _global.parseInt(_loc4_[1],36);
      var _loc7_ = _global.parseInt(_loc4_[2],36);
      var _loc8_ = _global.parseInt(_loc4_[3],36);
      var _loc9_ = _global.parseInt(_loc4_[4],36);
      var _loc10_ = _global.parseInt(_loc4_[5],36);
      if(this.api.datacenter.Player.guildInfos == undefined)
      {
         this.api.datacenter.Player.guildInfos = new dofus.datacenter.GuildInfos(_loc5_,_loc6_,_loc7_,_loc8_,_loc9_,_loc10_);
      }
      else
      {
         this.api.datacenter.Player.guildInfos.initialize(true,_loc5_,_loc6_,_loc7_,_loc8_,_loc9_,_loc10_);
      }
   }
   function onInfosGeneral(sExtraData)
   {
      var _loc3_ = sExtraData.split("|");
      var _loc4_ = _loc3_[0] == "1";
      var _loc5_ = Number(_loc3_[1]);
      var _loc6_ = Number(_loc3_[2]);
      var _loc7_ = Number(_loc3_[3]);
      var _loc8_ = Number(_loc3_[4]);
      this.api.datacenter.Player.guildInfos.setGeneralInfos(_loc4_,_loc5_,_loc6_,_loc7_,_loc8_);
      var _loc9_ = Number(_loc3_[5]);
      var _loc10_ = _loc3_[6];
      var _loc11_ = _loc3_[7];
      this.api.datacenter.Player.guildInfos.setNote(_loc11_,_loc10_,_loc9_);
   }
   function onInfosMembers(sExtraData)
   {
      var _loc3_ = sExtraData.charAt(0) == "+";
      var _loc4_ = sExtraData.substr(1).split("|");
      var _loc5_ = this.api.datacenter.Player.guildInfos;
      var _loc6_ = 0;
      var _loc7_;
      var _loc8_;
      var _loc9_;
      var _loc10_;
      var _loc11_;
      var _loc12_;
      var _loc13_;
      while(_loc6_ < _loc4_.length)
      {
         _loc7_ = _loc4_[_loc6_].split(";");
         _loc8_ = {};
         _loc8_.id = Number(_loc7_[0]);
         if(_loc3_)
         {
            _loc9_ = _loc5_.members.length == 0;
            _loc8_.name = _loc7_[1];
            _loc8_.level = Number(_loc7_[2]);
            _loc8_.gfx = Number(_loc7_[3]);
            _loc8_.rank = Number(_loc7_[4]);
            _loc8_.rankOrder = this.api.lang.getRankInfos(_loc8_.rank).o;
            _loc8_.winxp = Number(_loc7_[5]);
            _loc8_.percentxp = Number(_loc7_[6]);
            _loc8_.rights = new dofus.datacenter.GuildRights(Number(_loc7_[7]));
            _loc8_.state = Number(_loc7_[8]);
            _loc8_.alignement = Number(_loc7_[9]);
            _loc8_.lastConnection = Number(_loc7_[10]);
            _loc8_.hasTtgCollection = _loc7_[11] == "1";
            _loc8_.isLocalPlayer = _loc8_.id == this.api.datacenter.Player.ID;
            _loc10_ = _loc7_[12].split(",");
            _loc11_ = _loc7_[13];
            _loc8_.color1 = _loc10_[0];
            _loc8_.color2 = _loc10_[1];
            _loc8_.color3 = _loc10_[2];
            this.api.kernel.CharactersManager.setSpriteAccessories(_loc8_,_loc11_);
            if(_loc9_)
            {
               _loc5_.members.push(_loc8_);
            }
            else
            {
               _loc12_ = _loc5_.members.findFirstItem("id",_loc8_.id);
               if(_loc12_.index != -1)
               {
                  _loc5_.members.updateItem(_loc12_.index,_loc8_);
               }
               else
               {
                  _loc5_.members.push(_loc8_);
               }
            }
            _loc5_.members.sortOn("rankOrder",Array.NUMERIC);
         }
         else
         {
            _loc13_ = _loc5_.members.findFirstItem("id",_loc8_.id);
            if(_loc13_.index != -1)
            {
               _loc5_.members.removeItems(_loc13_.index,1);
            }
         }
         _loc6_ += 1;
      }
      _loc5_.setMembers();
   }
   function onInfosBoosts(sExtraData)
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
      if(sExtraData.length == 0)
      {
         this.api.datacenter.Player.guildInfos.setNoBoosts();
      }
      else
      {
         _loc3_ = sExtraData.split("|");
         _loc4_ = Number(_loc3_[0]);
         _loc5_ = Number(_loc3_[1]);
         _loc6_ = Number(_loc3_[2]);
         _loc7_ = Number(_loc3_[3]);
         _loc8_ = Number(_loc3_[4]);
         _loc9_ = Number(_loc3_[5]);
         _loc10_ = Number(_loc3_[6]);
         _loc3_.splice(0,7);
         _loc11_ = 0;
         while(_loc11_ < _loc3_.length)
         {
            _loc3_[_loc11_] = _loc3_[_loc11_].split(";");
            _loc11_ += 1;
         }
         _loc3_.sortOn("0");
         _loc12_ = new ank.utils.ExtendedArray();
         _loc13_ = 0;
         while(_loc13_ < _loc3_.length)
         {
            _loc14_ = Number(_loc3_[_loc13_][0]);
            _loc15_ = Number(_loc3_[_loc13_][1]);
            _loc12_.push(new dofus.datacenter.Spell(_loc14_,_loc15_));
            _loc13_ += 1;
         }
         this.api.datacenter.Player.guildInfos.setBoosts(_loc4_,_loc5_,_loc6_,_loc7_,_loc8_,_loc9_,_loc10_,_loc12_);
      }
   }
   function onInfosMountPark(sExtraData)
   {
      var _loc3_ = sExtraData.split("|");
      var _loc4_ = Number(_loc3_[0]);
      var _loc5_ = new ank.utils.ExtendedArray();
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
      while(_loc6_ < _loc3_.length)
      {
         _loc7_ = _loc3_[_loc6_].split(";");
         _loc8_ = Number(_loc7_[0]);
         _loc9_ = Number(_loc7_[1]);
         _loc10_ = _loc7_[2].split(",");
         _loc11_ = Number(_loc10_[0]);
         _loc12_ = Number(_loc10_[1]);
         _loc13_ = new dofus.datacenter.MountPark(0,-1,_loc9_,_loc11_,_loc12_,this.api.datacenter.Player.guildInfos.name,undefined,_loc8_);
         _loc13_.mounts = new ank.utils.ExtendedArray();
         if(_loc7_[3] != "")
         {
            _loc14_ = _loc7_[3].split(",");
            _loc15_ = 0;
            while(_loc15_ < _loc14_.length)
            {
               _loc16_ = new dofus.datacenter.Mount(Number(_loc14_[_loc15_]));
               _loc16_.name = _loc14_[_loc15_ + 1] != "" ? _loc14_[_loc15_ + 1] : this.api.lang.getText("NO_NAME");
               _loc16_.ownerName = _loc14_[_loc15_ + 2];
               _loc16_.sex = Number(_loc14_[_loc15_ + 3]);
               _loc13_.mounts.push(_loc16_);
               _loc15_ += 4;
            }
         }
         _loc13_.sortArea = _loc13_.areaName + _loc13_.subareaName;
         _loc13_.minMax = _loc13_.size;
         _loc13_.sortMounts = _loc13_.mounts.length;
         _loc5_.push(_loc13_);
         _loc6_ += 1;
      }
      this.api.datacenter.Player.guildInfos.setMountParks(_loc4_,_loc5_);
   }
   function onInfosTaxCollectorsMovement(sExtraData)
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
      if(sExtraData.length == 0)
      {
         this.api.datacenter.Player.guildInfos.setNoTaxCollectors();
      }
      else
      {
         _loc4_ = sExtraData.charAt(0) == "+";
         _loc5_ = sExtraData.substr(1).split("|");
         _loc6_ = this.api.datacenter.Player.guildInfos;
         _loc7_ = _loc5_[0].split(";");
         _loc6_.taxCount = Number(_loc7_[0]);
         _loc6_.taxCountMax = Number(_loc7_[1]);
         _loc6_.taxcollectorHireCost = Number(_loc7_[2]);
         _loc8_ = 1;
         while(_loc8_ < _loc5_.length)
         {
            _loc9_ = _loc5_[_loc8_].split(";");
            if(_loc9_.length < 2)
            {
               break;
            }
            _loc10_ = {};
            _loc10_.id = _global.parseInt(_loc9_[0],36);
            if(_loc4_)
            {
               _loc11_ = _loc6_.taxCollectors.length == 0;
               _loc12_ = _global.parseInt(_loc9_[2],36);
               _loc13_ = this.api.lang.getMapText(_loc12_);
               _loc14_ = this.api.lang.getMapAreaInfos(_loc13_.sa);
               _loc15_ = _loc13_.x + ", " + _loc13_.y;
               _loc16_ = this.api.lang.getMapAreaText(_loc14_.areaID).n;
               _loc17_ = this.api.lang.getMapSubAreaName(_loc13_.sa);
               if(dofus.Constants.DEBUG)
               {
                  _loc17_ += " (" + _loc12_ + ")";
               }
               _loc10_.name = this.api.lang.getFullNameText(_loc9_[1].split(","));
               _loc10_.mapID = _loc12_;
               _loc10_.areaName = _loc16_;
               _loc10_.subareaName = _loc17_;
               _loc10_.coordinates = _loc15_;
               _loc10_.state = Number(_loc9_[3]);
               _loc10_.timer = Number(_loc9_[4]);
               _loc10_.maxTimer = Number(_loc9_[5]);
               _loc10_.timerReference = getTimer();
               _loc10_.maxPlayerCount = Number(_loc9_[6]);
               _loc18_ = _loc9_[1].split(",");
               if(_loc18_.length != 2)
               {
                  _loc10_.showMoreInfo = true;
                  _loc10_.callerName = _loc18_[2] != "" ? _loc18_[2] : "?";
                  _loc10_.startDate = _global.parseInt(_loc18_[3],10);
                  _loc10_.lastHarvesterName = _loc18_[4] != "" ? _loc18_[4] : "?";
                  _loc10_.lastHarvestDate = _global.parseInt(_loc18_[5],10);
                  _loc10_.nextHarvestDate = _global.parseInt(_loc18_[6],10);
                  _loc10_.isMine = _loc18_[7] == this.api.datacenter.Player.ID;
               }
               else
               {
                  _loc10_.showMoreInfo = false;
                  _loc10_.callerName = "?";
                  _loc10_.startDate = -1;
                  _loc10_.lastHarvesterName = "?";
                  _loc10_.lastHarvestDate = -1;
                  _loc10_.nextHarvestDate = -1;
                  _loc10_.isMine = false;
               }
               _loc10_.sortArea = _loc16_ + _loc17_;
               _loc10_.sortState = _loc10_.state + _loc10_.isMine;
               _loc10_.players = new ank.utils.ExtendedArray();
               _loc10_.attackers = new ank.utils.ExtendedArray();
               if(_loc11_)
               {
                  _loc6_.taxCollectors.push(_loc10_);
               }
               else
               {
                  _loc19_ = _loc6_.taxCollectors.findFirstItem("id",_loc10_.id);
                  if(_loc19_.index != -1)
                  {
                     _loc6_.taxCollectors.updateItem(_loc19_.index,_loc10_);
                  }
                  else
                  {
                     _loc6_.taxCollectors.push(_loc10_);
                  }
               }
            }
            else
            {
               _loc20_ = _loc6_.taxCollectors.findFirstItem("id",_loc10_.id);
               if(_loc20_.index != -1)
               {
                  _loc6_.taxCollectors.removeItems(_loc20_.index,1);
               }
            }
            _loc8_ += 1;
         }
         _loc6_.setTaxCollectors();
      }
   }
   function onInfosTaxCollectorsPlayers(sExtraData)
   {
      var _loc4_ = sExtraData.charAt(0) == "+";
      var _loc5_ = sExtraData.substr(1).split("|");
      var _loc6_ = _global.parseInt(_loc5_[0],36);
      var _loc7_ = this.api.datacenter.Player.guildInfos.taxCollectors;
      var _loc8_ = _loc7_.findFirstItem("id",_loc6_);
      var _loc9_;
      var _loc10_;
      var _loc11_;
      var _loc12_;
      var _loc13_;
      var _loc14_;
      var _loc15_;
      if(_loc8_.index != -1)
      {
         _loc9_ = _loc8_.item;
         _loc10_ = 1;
         while(_loc10_ < _loc5_.length)
         {
            _loc11_ = _loc5_[_loc10_].split(";");
            if(_loc4_)
            {
               _loc12_ = {};
               _loc12_.id = _global.parseInt(_loc11_[0],36);
               _loc12_.name = _loc11_[1];
               _loc12_.gfxFile = dofus.Constants.CLIPS_PERSOS_PATH + _loc11_[2] + ".swf";
               _loc12_.level = Number(_loc11_[3]);
               _loc12_.color1 = _global.parseInt(_loc11_[4],36);
               _loc12_.color2 = _global.parseInt(_loc11_[5],36);
               _loc12_.color3 = _global.parseInt(_loc11_[6],36);
               _loc12_.prio = !!Number(_loc11_[7]);
               _loc13_ = _loc9_.players.findFirstItem("id",_loc12_.id);
               if(_loc13_.index != -1)
               {
                  _loc9_.players.updateItem(_loc13_.index,_loc12_);
               }
               else
               {
                  _loc9_.players.push(_loc12_);
               }
               if(_loc12_.id == this.api.datacenter.Player.ID)
               {
                  this.api.datacenter.Player.guildInfos.defendedTaxCollectorID = _loc6_;
               }
            }
            else
            {
               _loc14_ = _global.parseInt(_loc11_[0],36);
               _loc15_ = _loc9_.players.findFirstItem("id",_loc14_);
               if(_loc15_.index != -1)
               {
                  _loc9_.players.removeItems(_loc15_.index,1);
               }
               if(_loc14_ == this.api.datacenter.Player.ID)
               {
                  this.api.datacenter.Player.guildInfos.defendedTaxCollectorID = undefined;
               }
            }
            _loc10_ += 1;
         }
      }
      else
      {
         ank.utils.Logger.err("[gITP] impossible de trouver le percepteur");
      }
   }
   function onInfosTaxCollectorsAttackers(sExtraData)
   {
      var _loc4_ = sExtraData.charAt(0) == "+";
      var _loc5_ = sExtraData.substr(1).split("|");
      var _loc6_ = _global.parseInt(_loc5_[0],36);
      var _loc7_ = this.api.datacenter.Player.guildInfos.taxCollectors;
      var _loc8_ = _loc7_.findFirstItem("id",_loc6_);
      var _loc9_;
      var _loc10_;
      var _loc11_;
      var _loc12_;
      var _loc13_;
      var _loc14_;
      var _loc15_;
      if(_loc8_.index != -1)
      {
         _loc9_ = _loc8_.item;
         _loc10_ = 1;
         while(_loc10_ < _loc5_.length)
         {
            _loc11_ = _loc5_[_loc10_].split(";");
            if(_loc4_)
            {
               _loc12_ = {};
               _loc12_.id = _global.parseInt(_loc11_[0],36);
               _loc12_.name = _loc11_[1];
               _loc12_.level = Number(_loc11_[2]);
               _loc13_ = _loc9_.attackers.findFirstItem("id",_loc12_.id);
               if(_loc13_.index != -1)
               {
                  _loc9_.attackers.updateItem(_loc13_.index,_loc12_);
               }
               else
               {
                  _loc9_.attackers.push(_loc12_);
               }
            }
            else
            {
               _loc14_ = _global.parseInt(_loc11_[0],36);
               _loc15_ = _loc9_.attackers.findFirstItem("id",_loc14_);
               if(_loc15_.index != -1)
               {
                  _loc9_.attackers.removeItems(_loc15_.index,1);
               }
            }
            _loc10_ += 1;
         }
      }
      else
      {
         ank.utils.Logger.err("[gITp] impossible de trouver le percepteur");
      }
   }
   function onInfosHouses(sExtraData)
   {
      var _loc3_ = sExtraData.charAt(0) == "+";
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
      if(sExtraData.length <= 1)
      {
         this.api.datacenter.Player.guildInfos.setNoHouses();
      }
      else
      {
         _loc4_ = sExtraData.substr(1).split("|");
         _loc5_ = new ank.utils.ExtendedArray();
         _loc6_ = 0;
         while(_loc6_ < _loc4_.length)
         {
            _loc7_ = _loc4_[_loc6_].split(";");
            _loc8_ = Number(_loc7_[0]);
            _loc9_ = Number(_loc7_[1]);
            _loc10_ = _loc7_[2];
            _loc11_ = _loc7_[3].split(",");
            _loc12_ = new com.ankamagames.types.Point(Number(_loc11_[0]),Number(_loc11_[1]));
            _loc13_ = [];
            _loc14_ = _loc7_[4].split(",");
            _loc15_ = 0;
            while(_loc15_ < _loc14_.length)
            {
               _loc13_.push(Number(_loc14_[_loc15_]));
               _loc15_ += 1;
            }
            _loc16_ = _loc7_[5];
            _loc17_ = this.api.kernel.HouseManager.getHouseByInstance(_loc8_,_loc9_);
            _loc17_.ownerName = _loc10_;
            _loc17_.coords = _loc12_;
            _loc17_.skills = _loc13_;
            _loc17_.guildRights = _loc16_;
            _loc5_.push(_loc17_);
            _loc6_ += 1;
         }
         this.api.datacenter.Player.guildInfos.setHouses(_loc5_);
      }
   }
   function onInfos(sData)
   {
      var _loc3_ = sData.split("|");
      var _loc4_ = Number(_loc3_[0]);
      var _loc5_ = _loc3_[1];
      var _loc6_ = _loc3_[2];
      this.api.datacenter.Player.guildInfos.setInformations(_loc6_,_loc5_,_loc4_);
   }
   function onRequestLocal(sExtraData)
   {
      this.api.kernel.showMessage(this.api.lang.getText("GUILD"),this.api.lang.getText("YOU_INVIT_B_IN_GUILD",[sExtraData]),"INFO_CANCEL",{name:"Guild",listener:this,params:{spriteID:this.api.datacenter.Player.ID}});
   }
   function onRequestDistant(sExtraData)
   {
      var _loc3_ = sExtraData.split("|");
      var _loc4_ = _loc3_[0];
      var _loc5_ = _loc3_[1];
      var _loc6_ = _loc3_[2];
      if(this.api.kernel.ChatManager.isBlacklisted(_loc5_))
      {
         this.refuseInvitation(Number(_loc4_));
         return undefined;
      }
      this.api.electron.makeNotification(this.api.lang.getText("A_INVIT_YOU_IN_GUILD",[_loc5_,_loc6_]));
      this.api.kernel.showMessage(undefined,this.api.lang.getText("CHAT_A_INVIT_YOU_IN_GUILD",[this.api.kernel.ChatManager.getLinkName(_loc4_,_loc5_),_loc6_]),"INFO_CHAT");
      this.api.kernel.showMessage(this.api.lang.getText("GUILD"),this.api.lang.getText("A_INVIT_YOU_IN_GUILD",[_loc5_,_loc6_]),"CAUTION_YESNOIGNORE",{name:"Guild",player:_loc5_,listener:this,params:{spriteID:_loc4_,player:_loc5_}});
   }
   function onJoinError(sExtraData)
   {
      var _loc3_ = sExtraData.charAt(0);
      var _loc4_;
      switch(_loc3_)
      {
         case "a":
            this.api.kernel.showMessage(undefined,this.api.lang.getText("GUILD_JOIN_ALREADY_IN_GUILD"),"ERROR_CHAT");
            return;
         case "d":
            this.api.kernel.showMessage(undefined,this.api.lang.getText("GUILD_JOIN_NO_RIGHTS"),"ERROR_CHAT");
            return;
         case "u":
            this.api.kernel.showMessage(undefined,this.api.lang.getText("GUILD_JOIN_UNKNOW"),"ERROR_CHAT");
            return;
         case "o":
            this.api.kernel.showMessage(undefined,this.api.lang.getText("GUILD_JOIN_OCCUPED"),"ERROR_CHAT");
            return;
         case "r":
            _loc4_ = sExtraData.substr(1);
            this.api.kernel.showMessage(undefined,this.api.lang.getText("GUILD_JOIN_REFUSED",[_loc4_]),"ERROR_CHAT");
            this.api.ui.unloadUIComponent("AskCancelGuild");
            return;
         case "c":
            this.api.ui.unloadUIComponent("AskCancelGuild");
            this.api.ui.unloadUIComponent("AskYesNoIgnoreGuild");
      }
      return undefined;
   }
   function onJoinOk(sExtraData)
   {
      var _loc3_ = sExtraData.charAt(0);
      switch(_loc3_)
      {
         case "a":
            this.api.ui.unloadUIComponent("AskCancelGuild");
            this.api.kernel.showMessage(undefined,this.api.lang.getText("A_JOIN_YOUR_GUILD",[sExtraData.substr(1)]),"INFO_CHAT");
            return;
         case "j":
            this.api.kernel.showMessage(undefined,this.api.lang.getText("YOUR_R_NEW_IN_GUILD",[this.api.datacenter.Player.guildInfos.name]),"INFO_CHAT");
      }
      return undefined;
   }
   function onJoinDistantOk()
   {
      this.api.ui.unloadUIComponent("AskYesNoIgnoreGuild");
   }
   function onLeave()
   {
      this.api.ui.unloadUIComponent("CreateGuild");
   }
   function onBann(bSuccess, sExtraData)
   {
      var _loc4_;
      var _loc5_;
      var _loc6_;
      var _loc7_;
      var _loc8_;
      if(!bSuccess)
      {
         _loc8_ = sExtraData.charAt(0);
         switch(_loc8_)
         {
            case "d":
               this.api.kernel.showMessage(undefined,this.api.lang.getText("NOT_ENOUGHT_RIGHTS_FROM_GUILD"),"ERROR_CHAT");
               return;
            case "a":
               this.api.kernel.showMessage(undefined,this.api.lang.getText("CANT_BANN_FROM_GUILD_NOT_MEMBER"),"ERROR_CHAT");
         }
         return undefined;
      }
      _loc4_ = sExtraData.split("|");
      _loc5_ = _loc4_[0];
      _loc6_ = _loc4_[1];
      _loc7_ = _loc5_ == this.api.datacenter.Player.Name;
      if(_loc7_)
      {
         if(_loc5_ != _loc6_)
         {
            this.api.kernel.showMessage(undefined,this.api.lang.getText("YOU_BANN_A_FROM_GUILD",[_loc6_]),"INFO_CHAT");
         }
         else
         {
            this.api.kernel.showMessage(undefined,this.api.lang.getText("YOU_BANN_YOU_FROM_GUILD"),"INFO_CHAT");
            this.api.ui.unloadUIComponent("Guild");
            this.api.datacenter.Player.guildInfos = undefined;
         }
      }
      else
      {
         this.api.kernel.showMessage(undefined,this.api.lang.getText("YOU_ARE_BANN_BY_A_FROM_GUILD",[_loc5_]),"INFO_CHAT");
         this.api.ui.unloadUIComponent("Guild");
         delete this.api.datacenter.Player.guildInfos;
      }
   }
   function onHireTaxCollector(bSuccess, sExtraData)
   {
      var _loc4_;
      if(!bSuccess)
      {
         _loc4_ = sExtraData.charAt(0);
         switch(_loc4_)
         {
            case "d":
               this.api.kernel.showMessage(undefined,this.api.lang.getText("NOT_ENOUGHT_RIGHTS_FROM_GUILD"),"ERROR_CHAT");
               break;
            case "a":
               this.api.kernel.showMessage(undefined,this.api.lang.getText("ALREADY_TAXCOLLECTOR_ON_MAP"),"ERROR_CHAT");
               break;
            case "k":
               this.api.kernel.showMessage(undefined,this.api.lang.getText("NOT_ENOUGTH_RICH_TO_HIRE_TAX"),"ERROR_CHAT");
               break;
            case "m":
               this.api.kernel.showMessage(undefined,this.api.lang.getText("CANT_HIRE_MAX_TAXCOLLECTORS"),"ERROR_CHAT");
               break;
            case "b":
               this.api.kernel.showMessage(undefined,this.api.lang.getText("NOT_YOUR_TAXCOLLECTORS"),"ERROR_CHAT");
               break;
            case "y":
               this.api.kernel.showMessage(undefined,this.api.lang.getText("CANT_HIRE_TAXCOLLECTORS_TOO_TIRED"),"ERROR_CHAT");
               break;
            case "h":
               this.api.kernel.showMessage(undefined,this.api.lang.getText("CANT_HIRE_TAXCOLLECTORS_HERE"),"ERROR_CHAT");
            default:
               return undefined;
         }
      }
      else
      {
         this.getInfosTaxCollector();
      }
   }
   function onTaxCollectorAttacked(sExtraData)
   {
      var _loc3_ = sExtraData.split("|");
      var _loc4_ = _loc3_[0].charAt(0);
      var _loc5_ = this.api.lang.getFullNameText(_loc3_[0].substr(1).split(","));
      var _loc6_ = Number(_loc3_[1]);
      var _loc7_ = _loc3_[2];
      var _loc8_ = _loc3_[3];
      var _loc9_ = "(" + _loc7_ + ", " + _loc8_ + ")";
      switch(_loc4_)
      {
         case "A":
            this.api.electron.makeNotification(this.api.lang.getText("TAX_ATTACKED",[_loc5_,_loc9_]));
            this.api.kernel.showMessage(undefined,this.api.electron.getCautionIcon() + "<a href=\"asfunction:onHref,OpenGuildTaxCollectors\">" + this.api.lang.getText("TAX_ATTACKED",[_loc5_,_loc9_]) + "</a>","GUILD_CHAT");
            this.api.sounds.events.onTaxcollectorAttack();
            return;
         case "S":
            this.api.kernel.showMessage(undefined,this.api.lang.getText("TAX_ATTACKED_SUVIVED",[_loc5_,_loc9_]),"GUILD_CHAT");
            return;
         case "D":
            this.api.kernel.showMessage(undefined,this.api.lang.getText("TAX_ATTACKED_DIED",[_loc5_,_loc9_]),"GUILD_CHAT");
      }
      return undefined;
   }
   function onTaxCollectorInfo(sExtraData)
   {
      var _loc3_ = sExtraData.split("|");
      var _loc4_ = _loc3_[0].charAt(0);
      var _loc5_ = this.api.lang.getFullNameText(_loc3_[0].substr(1).split(","));
      var _loc6_ = Number(_loc3_[1]);
      var _loc7_ = _loc3_[2];
      var _loc8_ = _loc3_[3];
      var _loc9_ = "(" + _loc7_ + ", " + _loc8_ + ")";
      var _loc10_ = _loc3_[4];
      var _loc11_;
      var _loc12_;
      var _loc13_;
      var _loc14_;
      var _loc15_;
      var _loc16_;
      var _loc17_;
      switch(_loc4_)
      {
         case "S":
            this.api.kernel.showMessage(undefined,this.api.lang.getText("TAXCOLLECTOR_ADDED",[_loc5_,_loc9_,_loc10_]),"GUILD_CHAT");
            return;
         case "R":
            this.api.kernel.showMessage(undefined,this.api.lang.getText("TAXCOLLECTOR_REMOVED",[_loc5_,_loc9_,_loc10_]),"GUILD_CHAT");
            return;
         case "G":
            _loc11_ = _loc3_[5].split(";");
            _loc12_ = Number(_loc11_[0]);
            _loc13_ = _loc12_ + " " + this.api.lang.getText("EXPERIENCE_POINT");
            _loc14_ = 1;
            while(_loc14_ < _loc11_.length)
            {
               _loc15_ = _loc11_[_loc14_].split(",");
               _loc16_ = _loc15_[0];
               _loc17_ = _loc15_[1];
               _loc13_ += ",<br/>" + _loc17_ + " x " + this.api.lang.getItemUnicText(_loc16_).n;
               _loc14_ += 1;
            }
            _loc13_ += ".";
            this.api.kernel.showMessage(undefined,this.api.lang.getText("TAXCOLLECTOR_RECOLTED",[_loc5_,_loc9_,_loc10_,_loc13_]),"GUILD_CHAT");
      }
      return undefined;
   }
   function onUserInterfaceOpen(sExtraData)
   {
      switch(sExtraData)
      {
         case "T":
            if(this.api.datacenter.Player.guildInfos.name != undefined)
            {
               this.api.ui.loadUIAutoHideComponent("Guild","Guild",{currentTab:"GuildHouses"});
            }
            else
            {
               this.api.kernel.showMessage(undefined,this.api.lang.getText("ITEM_NEED_GUILD"),"ERROR_CHAT");
            }
            return;
         case "F":
            if(this.api.datacenter.Player.guildInfos.name != undefined)
            {
               this.api.ui.loadUIAutoHideComponent("Guild","Guild",{currentTab:"MountParks"});
               break;
            }
            this.api.kernel.showMessage(undefined,this.api.lang.getText("ITEM_NEED_GUILD"),"ERROR_CHAT");
      }
      return undefined;
   }
   function onRankNameChange(sData)
   {
      var _loc3_ = this.api.datacenter.Player.guildInfos;
      var _loc4_ = sData.split("|");
      var _loc5_ = 0;
      var _loc6_;
      var _loc7_;
      while(_loc5_ < _loc4_.length)
      {
         _loc6_ = _loc4_[_loc5_];
         _loc7_ = _loc6_.split(";");
         if(_loc7_[1] == "0")
         {
            _loc3_.resetRankName(Number(_loc7_[0]));
         }
         else
         {
            _loc3_.setRankName(Number(_loc7_[0]),_loc7_[1]);
         }
         _loc5_ += 1;
      }
   }
   function cancel(oEvent)
   {
      var _loc3_;
      if((_loc3_ = oEvent.target._name) === "AskCancelGuild")
      {
         this.refuseInvitation(oEvent.params.spriteID);
      }
   }
   function yes(oEvent)
   {
      var _loc3_;
      if((_loc3_ = oEvent.target._name) === "AskYesNoIgnoreGuild")
      {
         this.acceptInvitation(oEvent.params.spriteID);
      }
   }
   function no(oEvent)
   {
      var _loc3_;
      if((_loc3_ = oEvent.target._name) === "AskYesNoIgnoreGuild")
      {
         this.refuseInvitation(oEvent.params.spriteID);
      }
   }
   function ignore(oEvent)
   {
      var _loc3_;
      if((_loc3_ = oEvent.target._name) === "AskYesNoIgnoreGuild")
      {
         this.api.kernel.ChatManager.addToBlacklist(oEvent.params.player);
         this.api.kernel.showMessage(undefined,this.api.lang.getText("TEMPORARY_BLACKLISTED",[oEvent.params.player]),"INFO_CHAT");
         this.refuseInvitation(oEvent.params.spriteID);
      }
   }
}
