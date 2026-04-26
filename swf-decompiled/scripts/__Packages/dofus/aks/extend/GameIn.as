class dofus.aks.extend.GameIn extends dofus.aks.Handler
{
   var _clearOutOfFightMobGlowState;
   var _hardAuraVerifyIntervals;
   var _hardAuraVerifyRetries;
   var addToQueue;
   var aks;
   var api;
   var _aGameSpriteLeftHistory = [];
   var _hardAuraIntervals = {};
   var _hardAuraRetries = {};
   var _hardAuraApplied = {};
   var _hardAuraMapApplied = {};
   var _hardAuraMapLoading = {};
   function GameIn(oAKS, oAPI)
   {
      super.initialize(oAKS,oAPI);
      var self = this;
      _global._hardAuraReapplyCallback = function(nSpriteId)
      {
         self.reapplyOutOfFightMobFilter(nSpriteId);
      };
   }
   function onMovement(sExtraData, bIsSummoned)
   {
      var _loc5_ = sExtraData.split("|");
      var _loc6_ = _loc5_.length - 1;
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
      for(; _loc6_ >= 0; _loc6_ -= 1)
      {
         _loc7_ = _loc5_[_loc6_];
         if(_loc7_.length != 0)
         {
            _loc8_ = false;
            _loc9_ = false;
            _loc10_ = _loc7_.charAt(0);
            if(_loc10_ == "+")
            {
               _loc9_ = true;
            }
            else if(_loc10_ == "~")
            {
               _loc9_ = true;
               _loc8_ = true;
            }
            else if(_loc10_ != "-")
            {
               continue;
            }
            if(_loc9_)
            {
               _loc11_ = _loc7_.substr(1).split(";");
               _loc12_ = _loc11_[0];
               if(_loc12_ == "-1")
               {
                  _loc12_ = String(this.api.datacenter.Player.data.cellNum);
               }
               _loc13_ = _loc11_[1];
               _global.trace("[ORN] RAW _loc11_[2] = " + _loc11_[2]);
               _loc89_ = _loc11_[2].split("^");
               _global.trace("[ORN] split \'^\' length = " + _loc89_.length);
               _global.trace("[ORN] id = " + _loc89_[0]);
               _global.trace("[ORN] enabled = " + _loc89_[1]);
               _global.trace("[ORN] file = " + _loc89_[2]);
               _loc90_ = Number(_loc89_[0]);
               _loc91_ = _loc89_[1] == "1";
               _loc92_ = _loc89_[2];
               _global.trace("[ORN] nOrnamento = " + _loc90_);
               _global.trace("[ORN] activo = " + _loc91_);
               _global.trace("[ORN] sFile = " + _loc92_);
               _loc15_ = _loc11_[3];
               _loc16_ = _loc11_[4];
               _loc17_ = _loc11_[5];
               _loc18_ = _loc11_[6];
               _loc19_ = false;
               _loc20_ = true;
               if(_loc18_.charAt(_loc18_.length - 1) == "*")
               {
                  _loc18_ = _loc18_.substr(0,_loc18_.length - 1);
                  _loc19_ = true;
               }
               if(_loc18_.charAt(0) == "*")
               {
                  _loc20_ = false;
                  _loc18_ = _loc18_.substr(1);
               }
               _loc21_ = _loc18_.split("^");
               _loc22_ = _loc21_.length != 2 ? _loc18_ : _loc21_[0];
               _loc23_ = _loc17_.split(",");
               _loc24_ = _loc23_[0];
               _loc25_ = _loc23_[1];
               if(_loc25_.length)
               {
                  _loc27_ = _loc25_.split("~");
                  if(_loc27_[0].length > 0)
                  {
                     _loc28_ = _loc27_[0].split("*");
                     _loc26_ = new dofus.datacenter.Title(-1,_loc28_[1],_loc28_[0],1,"");
                  }
                  if(_loc27_[1].length > 0)
                  {
                     _loc29_ = _loc27_[1].split("*");
                     _loc30_ = new dofus.datacenter.Title(-1,_loc29_[0],_global.parseInt(_loc29_[1]));
                  }
               }
               _loc28_ = 100;
               _loc29_ = 100;
               if(_loc21_.length == 2)
               {
                  _loc30_ = _loc21_[1];
                  if(_global.isNaN(Number(_loc30_)))
                  {
                     _loc31_ = _loc30_.split("x");
                     _loc28_ = _loc31_.length != 2 ? 100 : Number(_loc31_[0]);
                     _loc29_ = _loc31_.length != 2 ? 100 : Number(_loc31_[1]);
                  }
                  else
                  {
                     _loc28_ = _loc29_ = Number(_loc30_);
                  }
               }
               if(_loc8_)
               {
                  _loc32_ = this.api.datacenter.Sprites.getItemAt(_loc15_);
                  this.onSpriteMovement(false,_loc32_);
               }
               switch(_loc24_)
               {
                  case "-1":
                  case "-2":
                     _loc33_ = {};
                     _loc33_.spriteType = _loc24_;
                     _loc33_.gfxID = _loc22_;
                     _loc33_.scaleX = _loc28_;
                     _loc33_.scaleY = _loc29_;
                     _loc33_.noFlip = _loc19_;
                     _loc33_.cell = _loc12_;
                     _loc33_.dir = _loc13_;
                     _loc33_.powerLevel = _loc11_[7];
                     _loc33_.color1 = _loc11_[8];
                     _loc33_.color2 = _loc11_[9];
                     _loc33_.color3 = _loc11_[10];
                     _loc33_.accessories = _loc11_[11];
                     if(this.api.datacenter.Game.isFight)
                     {
                        _loc33_.LP = _loc11_[12];
                        _loc33_.AP = _loc11_[13];
                        _loc33_.MP = _loc11_[14];
                        if(_loc11_.length > 18)
                        {
                           _loc33_.resistances = [Number(_loc11_[15]),Number(_loc11_[16]),Number(_loc11_[17]),Number(_loc11_[18]),Number(_loc11_[19]),Number(_loc11_[20]),Number(_loc11_[21])];
                           _loc33_.team = _loc11_[22];
                           _loc33_.LPmax = _loc11_[23];
                        }
                        else
                        {
                           _loc33_.team = _loc11_[15];
                           _loc33_.LPmax = _loc11_[16];
                        }
                        _loc33_.summoned = bIsSummoned;
                     }
                     if(_loc24_ == -1)
                     {
                        _loc32_ = this.api.kernel.CharactersManager.createCreature(_loc15_,_loc16_,_loc33_);
                        break;
                     }
                     _loc32_ = this.api.kernel.CharactersManager.createMonster(_loc15_,_loc16_,_loc33_);
                     break;
                  case "-3":
                     _loc34_ = {};
                     _loc34_.spriteType = _loc24_;
                     _loc34_.level = _loc11_[7];
                     _loc34_.scaleX = _loc28_;
                     _loc34_.scaleY = _loc29_;
                     _loc34_.noFlip = _loc19_;
                     _loc34_.cell = Number(_loc12_);
                     _loc34_.dir = _loc13_;
                     _loc35_ = _loc11_[8].split(",");
                     _loc34_.color1 = _loc35_[0];
                     _loc34_.color2 = _loc35_[1];
                     _loc34_.color3 = _loc35_[2];
                     _loc34_.accessories = _loc11_[9];
                     _loc34_.bonusValue = _loc90_;
                     _loc36_ = this.sliptGfxData(_loc18_);
                     _loc37_ = _loc36_.gfx;
                     this.splitGfxForScale(_loc37_[0],_loc34_);
                     _loc32_ = this.api.kernel.CharactersManager.createMonsterGroup(_loc15_,_loc16_,_loc34_);
                     if(this.api.kernel.OptionsManager.getOption("ViewAllMonsterInGroup") == true)
                     {
                        _loc38_ = _loc15_;
                        _loc39_ = 1;
                        while(_loc39_ < _loc37_.length)
                        {
                           if(_loc37_[_loc39_] != "")
                           {
                              this.splitGfxForScale(_loc37_[_loc39_],_loc34_);
                              _loc35_ = _loc11_[8 + 2 * _loc39_].split(",");
                              _loc34_.color1 = _loc35_[0];
                              _loc34_.color2 = _loc35_[1];
                              _loc34_.color3 = _loc35_[2];
                              _loc34_.dir = random(4) * 2 + 1;
                              _loc34_.accessories = _loc11_[9 + 2 * _loc39_];
                              _loc40_ = _loc15_ + "_" + _loc39_;
                              _loc41_ = this.api.kernel.CharactersManager.createMonsterGroup(_loc40_,undefined,_loc34_);
                              _loc42_ = _loc38_;
                              if(random(3) != 0 && _loc39_ != 1)
                              {
                                 _loc42_ = _loc15_ + "_" + (random(_loc39_ - 1) + 1);
                              }
                              _loc43_ = random(8);
                              this.api.gfx.addLinkedSprite(_loc40_,_loc42_,_loc43_,_loc41_);
                              if(_loc32_ != undefined && _loc32_._aTemplateIds != undefined)
                              {
                                 _loc44_ = _loc32_._aTemplateIds;
                                 _loc45_ = _loc44_[_loc39_];
                                 if(_loc45_ != undefined)
                                 {
                                    _loc41_._aTemplateIds = [Number(_loc45_)];
                                    this._hardAura_applyNow(_loc41_,"GROUP_MEMBER_CREATE");
                                 }
                              }
                              if(!_global.isNaN(_loc41_.scaleX))
                              {
                                 this.api.gfx.setSpriteScale(_loc41_.id,_loc41_.scaleX,_loc41_.scaleY);
                              }
                              switch(_loc36_.shape)
                              {
                                 case "circle":
                                    _loc43_ = _loc39_;
                                    break;
                                 case "line":
                                    _loc42_ = _loc40_;
                                    _loc43_ = 2;
                              }
                           }
                           _loc39_ += 1;
                        }
                     }
                     break;
                  case "-4":
                     _loc46_ = {};
                     _loc46_.spriteType = _loc24_;
                     _loc46_.gfxID = _loc22_;
                     _loc46_.scaleX = _loc28_;
                     _loc46_.scaleY = _loc29_;
                     _loc46_.cell = _loc12_;
                     _loc46_.dir = _loc13_;
                     _loc46_.sex = _loc11_[7];
                     _loc46_.color1 = _loc11_[8];
                     _loc46_.color2 = _loc11_[9];
                     _loc46_.color3 = _loc11_[10];
                     _loc46_.accessories = _loc11_[11];
                     _loc46_.extraClipID = !(_loc11_[12] != undefined && !_global.isNaN(Number(_loc11_[12]))) ? -1 : Number(_loc11_[12]);
                     _loc46_.customArtwork = Number(_loc11_[13]);
                     _loc32_ = this.api.kernel.CharactersManager.createNonPlayableCharacter(_loc15_,Number(_loc16_),_loc46_);
                     break;
                  case "-5":
                     _loc47_ = {};
                     _loc47_.spriteType = _loc24_;
                     _loc47_.gfxID = _loc22_;
                     _loc47_.scaleX = _loc28_;
                     _loc47_.scaleY = _loc29_;
                     _loc47_.cell = _loc12_;
                     _loc47_.dir = _loc13_;
                     _loc47_.color1 = _loc11_[7];
                     _loc47_.color2 = _loc11_[8];
                     _loc47_.color3 = _loc11_[9];
                     _loc47_.accessories = _loc11_[10];
                     _loc47_.guildName = _loc11_[11];
                     _loc47_.emblem = _loc11_[12];
                     _loc47_.offlineType = _loc11_[13];
                     _loc47_.characterID = _loc11_[14];
                     _loc32_ = this.api.kernel.CharactersManager.createOfflineCharacter(_loc15_,_loc16_,_loc47_);
                     break;
                  case "-6":
                     _loc48_ = {};
                     _loc48_.spriteType = _loc24_;
                     _loc48_.gfxID = _loc22_;
                     _loc48_.scaleX = _loc28_;
                     _loc48_.scaleY = _loc29_;
                     _loc48_.cell = _loc12_;
                     _loc48_.dir = _loc13_;
                     _loc48_.level = _loc11_[7];
                     if(this.api.datacenter.Game.isFight)
                     {
                        _loc48_.LP = _loc11_[8];
                        _loc48_.AP = _loc11_[9];
                        _loc48_.MP = _loc11_[10];
                        _loc48_.resistances = [Number(_loc11_[11]),Number(_loc11_[12]),Number(_loc11_[13]),Number(_loc11_[14]),Number(_loc11_[15]),Number(_loc11_[16]),Number(_loc11_[17])];
                        _loc48_.team = _loc11_[18];
                        _loc48_.LPmax = _loc11_[19];
                     }
                     else
                     {
                        _loc48_.guildName = _loc11_[8];
                        _loc48_.emblem = _loc11_[9];
                        _loc48_.isMine = !!Number(_loc11_[10]);
                     }
                     _loc32_ = this.api.kernel.CharactersManager.createTaxCollector(_loc15_,_loc16_,_loc48_);
                     break;
                  case "-7":
                  case "-8":
                     _loc49_ = {};
                     _loc49_.spriteType = _loc24_;
                     _loc49_.gfxID = _loc22_;
                     _loc49_.scaleX = _loc28_;
                     _loc49_.scaleY = _loc29_;
                     _loc49_.cell = _loc12_;
                     _loc49_.dir = _loc13_;
                     _loc49_.sex = _loc11_[7];
                     _loc49_.powerLevel = _loc11_[8];
                     _loc49_.accessories = _loc11_[9];
                     if(this.api.datacenter.Game.isFight)
                     {
                        _loc49_.LP = _loc11_[10];
                        _loc49_.AP = _loc11_[11];
                        _loc49_.MP = _loc11_[12];
                        _loc49_.team = _loc11_[20];
                        _loc49_.LPmax = _loc11_[21];
                     }
                     else
                     {
                        _loc49_.emote = _loc11_[10];
                        _loc49_.emoteTimer = _loc11_[11];
                        _loc49_.restrictions = Number(_loc11_[12]);
                     }
                     if(_loc24_ == "-8")
                     {
                        _loc49_.showIsPlayer = true;
                        _loc50_ = _loc16_.split("~");
                        _loc49_.monsterID = _loc50_[0];
                        _loc49_.playerName = _loc50_[1];
                        _loc49_.team = _loc11_[13];
                     }
                     else
                     {
                        _loc49_.showIsPlayer = false;
                        _loc49_.monsterID = _loc16_;
                     }
                     _loc32_ = this.api.kernel.CharactersManager.createMutant(_loc15_,_loc49_);
                     break;
                  case "-9":
                     _loc51_ = {};
                     _loc51_.spriteType = _loc24_;
                     _loc51_.gfxID = _loc22_;
                     _loc51_.scaleX = _loc28_;
                     _loc51_.scaleY = _loc29_;
                     _loc51_.cell = _loc12_;
                     _loc51_.dir = _loc13_;
                     _loc51_.ownerName = _loc11_[7];
                     _loc51_.level = _loc11_[8];
                     _loc51_.modelID = _loc11_[9];
                     _loc32_ = this.api.kernel.CharactersManager.createParkMount(_loc15_,_loc16_ == "" ? this.api.lang.getText("NO_NAME") : _loc16_,_loc51_);
                     break;
                  case "-10":
                     _loc52_ = {};
                     _loc52_.spriteType = _loc24_;
                     _loc52_.gfxID = _loc22_;
                     _loc52_.scaleX = _loc28_;
                     _loc52_.scaleY = _loc29_;
                     _loc52_.cell = _loc12_;
                     _loc52_.dir = _loc13_;
                     _loc52_.level = _loc11_[7];
                     _loc52_.alignment = new dofus.datacenter.Alignment(Number(_loc11_[9]),Number(_loc11_[8]));
                     _loc32_ = this.api.kernel.CharactersManager.createPrism(_loc15_,_loc16_,_loc52_);
                     break;
                  default:
                     _loc53_ = {};
                     _loc53_.spriteType = _loc24_;
                     _loc53_.cell = _loc12_;
                     _loc53_.scaleX = _loc28_;
                     _loc53_.scaleY = _loc29_;
                     _loc53_.dir = _loc13_;
                     _loc53_.sex = _loc11_[7];
                     if(this.api.datacenter.Game.isFight)
                     {
                        _loc53_.level = _loc11_[8];
                        _loc54_ = _loc11_[9];
                        _loc53_.color1 = _loc11_[10];
                        _loc53_.color2 = _loc11_[11];
                        _loc53_.color3 = _loc11_[12];
                        _loc53_.accessories = _loc11_[13];
                        _loc53_.LP = _loc11_[14];
                        _loc53_.AP = _loc11_[15];
                        _loc53_.MP = _loc11_[16];
                        _loc53_.resistances = [Number(_loc11_[17]),Number(_loc11_[18]),Number(_loc11_[19]),Number(_loc11_[20]),Number(_loc11_[21]),Number(_loc11_[22]),Number(_loc11_[23])];
                        _loc53_.team = _loc11_[24];
                        _loc53_.hasCandy = _loc11_[26];
                        _loc53_.hasBuff = _loc11_[27];
                        if(_loc11_[25].indexOf(",") != -1)
                        {
                           _loc55_ = _loc11_[25].split(",");
                           _loc56_ = Number(_loc55_[0]);
                           _loc57_ = _global.parseInt(_loc55_[1],16);
                           _loc58_ = _global.parseInt(_loc55_[2],16);
                           _loc59_ = _global.parseInt(_loc55_[3],16);
                           if(_loc57_ == -1 || _global.isNaN(_loc57_))
                           {
                              _loc57_ = this.api.datacenter.Player.color1;
                           }
                           if(_loc58_ == -1 || _global.isNaN(_loc58_))
                           {
                              _loc58_ = this.api.datacenter.Player.color2;
                           }
                           if(_loc59_ == -1 || _global.isNaN(_loc59_))
                           {
                              _loc59_ = this.api.datacenter.Player.color3;
                           }
                           if(!_global.isNaN(_loc56_))
                           {
                              _loc60_ = new dofus.datacenter.Mount(_loc56_,Number(_loc22_));
                              _loc60_.customColor1 = _loc57_;
                              _loc60_.customColor2 = _loc58_;
                              _loc60_.customColor3 = _loc59_;
                              _loc53_.mount = _loc60_;
                           }
                        }
                        else
                        {
                           _loc61_ = Number(_loc11_[25]);
                           if(!_global.isNaN(_loc61_))
                           {
                              _loc53_.mount = new dofus.datacenter.Mount(_loc61_,Number(_loc22_));
                           }
                        }
                        _loc53_.LPmax = _loc11_[28];
                        if(this.api.datacenter.Player.ID == _loc15_)
                        {
                           this.api.datacenter.Player.LPmax = _loc53_.LPmax;
                           this.api.datacenter.Player.LP = _loc53_.LP;
                        }
                     }
                     else
                     {
                        _loc54_ = _loc11_[8];
                        if(_loc90_ > 0)
                        {
                           _global.trace("[ORN] APPLY ornamento = " + _loc90_);
                           _loc53_.ornamento = _loc90_;
                        }
                        else
                        {
                           _global.trace("[ORN] IGNORE ornamento = 0 (mantener actual)");
                        }
                        _global.trace("[ORN] SET character ornamento | id=" + _loc15_ + " | ornamento=" + _loc90_ + " | activo=" + _loc91_);
                        _loc53_.color1 = _loc11_[9];
                        _loc53_.color2 = _loc11_[10];
                        _loc53_.color3 = _loc11_[11];
                        _loc53_.accessories = _loc11_[12];
                        _loc53_.aura = _loc11_[13];
                        _loc53_.emote = _loc11_[14];
                        _loc53_.emoteTimer = _loc11_[15];
                        _loc53_.guildName = _loc11_[16];
                        _loc53_.emblem = _loc11_[17];
                        _loc53_.restrictions = _loc11_[18];
                        _loc53_.hasTtgCollection = _loc11_[21] == "1";
                        if(_loc11_[19].indexOf(",") != -1)
                        {
                           _loc62_ = _loc11_[19].split(",");
                           _loc63_ = Number(_loc62_[0]);
                           _loc64_ = _global.parseInt(_loc62_[1],16);
                           _loc65_ = _global.parseInt(_loc62_[2],16);
                           _loc66_ = _global.parseInt(_loc62_[3],16);
                           if(_loc64_ == -1 || _global.isNaN(_loc64_))
                           {
                              _loc64_ = this.api.datacenter.Player.color1;
                           }
                           if(_loc65_ == -1 || _global.isNaN(_loc65_))
                           {
                              _loc65_ = this.api.datacenter.Player.color2;
                           }
                           if(_loc66_ == -1 || _global.isNaN(_loc66_))
                           {
                              _loc66_ = this.api.datacenter.Player.color3;
                           }
                           if(!_global.isNaN(_loc63_))
                           {
                              _loc67_ = new dofus.datacenter.Mount(_loc63_,Number(_loc22_));
                              _loc67_.customColor1 = _loc64_;
                              _loc67_.customColor2 = _loc65_;
                              _loc67_.customColor3 = _loc66_;
                              _loc53_.mount = _loc67_;
                           }
                        }
                        else
                        {
                           _loc68_ = Number(_loc11_[19]);
                           if(!_global.isNaN(_loc68_))
                           {
                              _loc53_.mount = new dofus.datacenter.Mount(_loc68_,Number(_loc22_));
                           }
                        }
                     }
                     if(_loc8_)
                     {
                        _loc69_ = [_loc15_,this.createTransitionEffect(),_loc12_,10];
                     }
                     _loc70_ = _loc54_.split(",");
                     _loc53_.alignment = new dofus.datacenter.Alignment(Number(_loc70_[0]),Number(_loc70_[1]));
                     _loc53_.rank = new dofus.datacenter.Rank(Number(_loc70_[2]));
                     _loc53_.alignment.fallenAngelDemon = _loc70_[4] == 1;
                     if(_loc70_.length > 3 && _loc15_ != this.api.datacenter.Player.ID)
                     {
                        if(this.api.lang.getAlignmentCanViewPvpGain(this.api.datacenter.Player.alignment.index,Number(_loc53_.alignment.index)))
                        {
                           _loc71_ = Number(_loc70_[3]) - _global.parseInt(_loc15_);
                           _loc72_ = this.api.lang.getConfigText("PVP_VIEW_BONUS_MINOR_LIMIT");
                           _loc73_ = this.api.lang.getConfigText("PVP_VIEW_BONUS_MINOR_LIMIT_PRC");
                           _loc74_ = this.api.lang.getConfigText("PVP_VIEW_BONUS_MAJOR_LIMIT");
                           _loc75_ = this.api.lang.getConfigText("PVP_VIEW_BONUS_MAJOR_LIMIT_PRC");
                           _loc76_ = 0;
                           if(this.api.datacenter.Player.Level * (1 - _loc73_ / 100) > _loc71_)
                           {
                              _loc76_ = -1;
                           }
                           if(this.api.datacenter.Player.Level - _loc71_ > _loc72_)
                           {
                              _loc76_ = -1;
                           }
                           if(this.api.datacenter.Player.Level * (1 + _loc75_ / 100) < _loc71_)
                           {
                              _loc76_ = 1;
                           }
                           if(this.api.datacenter.Player.Level - _loc71_ < _loc74_)
                           {
                              _loc76_ = 1;
                           }
                           _loc53_.pvpGain = _loc76_;
                        }
                     }
                     if(!this.api.datacenter.Game.isFight && (_global.parseInt(_loc15_,10) != this.api.datacenter.Player.ID && ((this.api.datacenter.Player.alignment.index == 1 || this.api.datacenter.Player.alignment.index == 2) && ((_loc53_.alignment.index == 1 || _loc53_.alignment.index == 2) && (_loc53_.alignment.index != this.api.datacenter.Player.alignment.index && (_loc53_.rank.value && this.api.datacenter.Map.bCanAttack))))))
                     {
                        if(this.api.datacenter.Player.rank.value > _loc53_.rank.value)
                        {
                           this.api.kernel.SpeakingItemsManager.triggerEvent(dofus.managers.SpeakingItemsManager.SPEAK_TRIGGER_NEW_ENEMY_WEAK);
                        }
                        if(this.api.datacenter.Player.rank.value < _loc53_.rank.value)
                        {
                           this.api.kernel.SpeakingItemsManager.triggerEvent(dofus.managers.SpeakingItemsManager.SPEAK_TRIGGER_NEW_ENEMY_STRONG);
                        }
                     }
                     _loc77_ = this.sliptGfxData(_loc18_);
                     _loc78_ = _loc77_.gfx;
                     this.splitGfxForScale(_loc78_[0],_loc53_);
                     _loc53_.title = _loc26_;
                     _loc32_ = this.api.kernel.CharactersManager.createCharacter(_loc15_,_loc16_,_loc53_);
                     dofus.datacenter.Character(_loc32_).isClear = false;
                     _loc32_.allowGhostMode = _loc20_;
                     _loc79_ = _loc15_;
                     _loc80_ = _loc77_.shape != "circle" ? 2 : 0;
                     _loc81_ = 1;
                     while(_loc81_ < _loc78_.length)
                     {
                        if(_loc78_[_loc81_] != "")
                        {
                           _loc82_ = _loc15_ + "_" + _loc81_;
                           _loc83_ = {};
                           this.splitGfxForScale(_loc78_[_loc81_],_loc83_);
                           _loc84_ = new ank.battlefield.datacenter.Sprite(_loc82_,ank.battlefield.mc.Sprite,dofus.Constants.CLIPS_PERSOS_PATH + _loc83_.gfxID + ".swf");
                           _loc84_.allDirections = false;
                           this.api.gfx.addLinkedSprite(_loc82_,_loc79_,_loc80_,_loc84_);
                           if(!_global.isNaN(_loc83_.scaleX))
                           {
                              this.api.gfx.setSpriteScale(_loc84_.id,_loc83_.scaleX,_loc83_.scaleY);
                           }
                           switch(_loc77_.shape)
                           {
                              case "circle":
                                 _loc80_ = _loc81_;
                                 break;
                              case "line":
                                 _loc79_ = _loc82_;
                                 _loc80_ = 2;
                           }
                        }
                        _loc81_ += 1;
                     }
               }
               this.onSpriteMovement(_loc9_,_loc32_,_loc69_);
            }
            else
            {
               _loc85_ = _loc7_.substr(1);
               _loc86_ = this.api.datacenter.Sprites.getItemAt(_loc85_);
               if(!this.api.datacenter.Game.isRunning && this.api.datacenter.Game.isLoggingMapDisconnections)
               {
                  _loc87_ = _loc86_.name;
                  _loc88_ = this._aGameSpriteLeftHistory[_loc85_];
                  if(!_global.isNaN(_loc88_) && getTimer() - _loc88_ < 300)
                  {
                     this.api.kernel.showMessage(undefined,this.api.kernel.DebugManager.getTimestamp() + " (Map) " + this.api.kernel.ChatManager.getLinkName(_loc85_,_loc87_) + " s\'est déconnecté (" + _loc85_ + ")","ADMIN_CHAT");
                  }
                  this._aGameSpriteLeftHistory[_loc85_] = getTimer();
               }
               this.onSpriteMovement(_loc9_,_loc86_);
            }
         }
      }
   }
   function onCellData(sExtraData)
   {
      var _loc3_ = sExtraData.split("|");
      var _loc4_ = 0;
      var _loc5_;
      var _loc6_;
      var _loc7_;
      var _loc8_;
      var _loc9_;
      while(_loc4_ < _loc3_.length)
      {
         _loc5_ = _loc3_[_loc4_].split(";");
         _loc6_ = Number(_loc5_[0]);
         _loc7_ = _loc5_[1].substring(0,10);
         _loc8_ = _loc5_[1].substr(10);
         _loc9_ = _loc5_[2] != "0" ? 1 : 0;
         this.api.gfx.updateCell(_loc6_,_loc7_,_loc8_,_loc9_);
         _loc4_ += 1;
      }
   }
   function onZoneData(sExtraData)
   {
      var _loc3_ = sExtraData.split("|");
      var _loc4_ = 0;
      var _loc5_;
      var _loc6_;
      var _loc7_;
      var _loc8_;
      var _loc9_;
      var _loc10_;
      var _loc11_;
      while(_loc4_ < _loc3_.length)
      {
         _loc5_ = _loc3_[_loc4_];
         _loc6_ = _loc5_.charAt(0) != "+" ? false : true;
         _loc7_ = _loc5_.substr(1).split(";");
         _loc8_ = Number(_loc7_[0]);
         _loc9_ = Number(_loc7_[1]);
         _loc10_ = _loc7_[2];
         _loc11_ = Number(_loc7_[3]);
         if(_loc6_)
         {
            this.api.gfx.drawZone(_loc8_,0,_loc9_,_loc10_,dofus.Constants.ZONE_COLOR[_loc10_],_loc11_);
         }
         else
         {
            this.api.gfx.clearZone(_loc8_,_loc9_,_loc10_);
         }
         _loc4_ += 1;
      }
   }
   function onCellObject(sExtraData)
   {
      var _loc4_ = sExtraData.charAt(0) == "+";
      var _loc5_ = sExtraData.substr(1).split("|");
      var _loc6_ = 0;
      var _loc7_;
      var _loc8_;
      var _loc9_;
      var _loc10_;
      var _loc11_;
      var _loc12_;
      while(_loc6_ < _loc5_.length)
      {
         _loc7_ = _loc5_[_loc6_].split(";");
         _loc8_ = Number(_loc7_[0]);
         _loc9_ = _global.parseInt(_loc7_[1]);
         if(_loc4_)
         {
            _loc10_ = new dofus.datacenter.Item(0,_loc9_);
            _loc11_ = Number(_loc7_[2]);
            switch(_loc11_)
            {
               case 0:
                  this.api.gfx.updateCellObjectExternalWithExternalClip(_loc8_,_loc10_.iconFile,1,true,true,_loc10_);
                  break;
               case 1:
                  if(this.api.gfx.mapHandler.getCellData(_loc8_).layerObjectExternalData.unicID != _loc9_)
                  {
                     this.api.gfx.updateCellObjectExternalWithExternalClip(_loc8_,_loc10_.iconFile,1,true,false,_loc10_);
                  }
                  else
                  {
                     _loc10_ = this.api.gfx.mapHandler.getCellData(_loc8_).layerObjectExternalData;
                  }
                  _loc10_.rideItemDurability = Number(_loc7_[3]);
                  _loc10_.rideItemDurabilityMax = Number(_loc7_[4]);
            }
         }
         else
         {
            _loc12_ = this.api.gfx.mapHandler.getCellData(_loc8_);
            if(_loc12_ != undefined && (_loc12_.mcObjectExternal != undefined && _loc12_.mcObjectExternal == this.api.gfx.rollOverMcObject))
            {
               this.api.gfx.onObjectRollOut(_loc12_.mcObjectExternal);
            }
            this.api.gfx.initializeCell(_loc8_,1);
         }
         _loc6_ += 1;
      }
   }
   function onFrameObject2(sExtraData)
   {
      var _loc3_ = ank.gapi.controls.PopupMenu.currentPopupMenu;
      var _loc4_ = sExtraData.split("|");
      var _loc5_ = 0;
      var _loc6_;
      var _loc7_;
      var _loc8_;
      var _loc9_;
      var _loc10_;
      while(_loc5_ < _loc4_.length)
      {
         _loc6_ = _loc4_[_loc5_].split(";");
         _loc7_ = Number(_loc6_[0]);
         _loc8_ = _loc6_[1];
         _loc9_ = _loc6_[2] != undefined;
         _loc10_ = _loc6_[2] != "1" ? false : true;
         if(_loc3_ != undefined && (_loc3_.gatherCellNum == _loc7_ && (!_loc10_ && _loc8_ == "3")))
         {
            _loc3_.removePopupMenu();
         }
         if(_loc9_)
         {
            this.api.gfx.setObject2Interactive(_loc7_,_loc10_,2);
         }
         this.api.gfx.setObject2Frame(_loc7_,_loc8_);
         _loc5_ += 1;
      }
   }
   function onFrameObjectExternal(sExtraData)
   {
      var _loc3_ = sExtraData.split("|");
      var _loc4_ = 0;
      var _loc5_;
      var _loc6_;
      var _loc7_;
      while(_loc4_ < _loc3_.length)
      {
         _loc5_ = _loc3_[_loc4_].split(";");
         _loc6_ = Number(_loc5_[0]);
         _loc7_ = Number(_loc5_[1]);
         this.api.gfx.setObjectExternalFrame(_loc6_,_loc7_);
         _loc4_ += 1;
      }
   }
   function onEffect(sExtraData)
   {
      var _loc3_ = sExtraData.split(";");
      var _loc4_ = _loc3_[0];
      var _loc5_ = _loc3_[1].split(",");
      var _loc6_ = _loc3_[2];
      var _loc7_ = _loc3_[3];
      var _loc8_ = _loc3_[4];
      var _loc9_ = _loc3_[5];
      var _loc10_ = Number(_loc3_[6]);
      var _loc11_ = _loc3_[7];
      var _loc12_ = _loc3_[8];
      var _loc13_ = Number(_loc3_[9]) == 1;
      var _loc14_ = 0;
      var _loc15_;
      var _loc16_;
      var _loc17_;
      while(_loc14_ < _loc5_.length)
      {
         _loc15_ = _loc5_[_loc14_];
         if(_loc15_ == this.api.datacenter.Game.currentPlayerID && _loc10_ != -1)
         {
            _loc10_ += 1;
         }
         _loc16_ = new dofus.datacenter.Effect(_loc12_,Number(_loc4_),Number(_loc6_),Number(_loc7_),Number(_loc8_),_loc9_,Number(_loc10_),Number(_loc11_),undefined,undefined,_loc13_);
         _loc17_ = this.api.datacenter.Sprites.getItemAt(_loc15_);
         _loc17_.EffectsManager.addEffect(_loc16_);
         _loc14_ += 1;
      }
   }
   function onClearAllEffect(sExtraData)
   {
      var _loc3_ = this.api.datacenter.Sprites;
      for(var _loc4_ in _loc3_)
      {
         _loc3_[_loc4_].EffectsManager.terminateAllEffects();
      }
   }
   function onChallenge(sExtraData)
   {
      var _loc3_ = sExtraData.charAt(0) == "+";
      var _loc4_ = sExtraData.substr(1).split("|");
      var _loc5_ = _loc4_.shift().split(";");
      var _loc6_ = Number(_loc5_[0]);
      var _loc7_ = Number(_loc5_[1]);
      var _loc8_ = (Math.cos(_loc6_) + 1) * 8388607;
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
      if(_loc3_)
      {
         _loc9_ = new dofus.datacenter.Challenge(_loc6_,_loc7_);
         this.api.datacenter.Challenges.addItemAt(_loc6_,_loc9_);
         _loc10_ = 0;
         while(_loc10_ < _loc4_.length)
         {
            _loc11_ = _loc4_[_loc10_].split(";");
            _loc12_ = _loc11_[0];
            _loc13_ = Number(_loc11_[1]);
            _loc14_ = Number(_loc11_[2]);
            _loc15_ = Number(_loc11_[3]);
            _loc16_ = dofus.Constants.getTeamFileFromType(_loc14_,_loc15_);
            _loc17_ = new dofus.datacenter.Team(_loc12_,ank.battlefield.mc.Sprite,_loc16_,_loc13_,_loc8_,_loc14_,_loc15_);
            _loc9_.addTeam(_loc17_);
            this.api.gfx.addSprite(_loc17_.id,_loc17_);
            _loc10_ += 1;
         }
      }
      else
      {
         _loc18_ = this.api.datacenter.Challenges.getItemAt(_loc6_).teams;
         for(var _loc20_ in _loc18_)
         {
            _loc19_ = _loc18_[_loc20_];
            this.api.gfx.removeSprite(_loc19_.id);
         }
         this.api.datacenter.Challenges.removeItemAt(_loc6_);
      }
   }
   function onTeam(sExtraData)
   {
      var _loc4_ = sExtraData.split("|");
      var _loc5_ = Number(_loc4_.shift());
      var _loc6_ = dofus.datacenter.Team(this.api.datacenter.Sprites.getItemAt(_loc5_));
      var _loc7_ = 0;
      var _loc8_;
      var _loc9_;
      var _loc10_;
      var _loc11_;
      var _loc12_;
      var _loc13_;
      var _loc14_;
      var _loc15_;
      while(_loc7_ < _loc4_.length)
      {
         _loc8_ = _loc4_[_loc7_].split(";");
         _loc9_ = _loc8_[0].charAt(0) == "+";
         _loc10_ = _loc8_[0].substr(1);
         _loc11_ = _loc8_[1];
         _loc12_ = _loc8_[2];
         _loc13_ = _loc11_.split(",");
         _loc14_ = Number(_loc11_);
         if(_loc13_.length > 1)
         {
            _loc11_ = this.api.lang.getFullNameText(_loc13_);
         }
         else if(!_global.isNaN(_loc14_))
         {
            _loc11_ = this.api.lang.getMonstersText(_loc14_).n;
         }
         if(_loc9_)
         {
            _loc15_ = {};
            _loc15_.id = _loc10_;
            _loc15_.name = _loc11_;
            _loc15_.level = _loc12_;
            _loc6_.addPlayer(_loc15_);
         }
         else
         {
            _loc6_.removePlayer(_loc10_);
         }
         _loc7_ += 1;
      }
      _loc6_.refreshSwordSprite();
   }
   function onFightOption(sExtraData)
   {
      var _loc3_ = sExtraData.substr(2);
      var _loc4_ = this.api.datacenter.Sprites.getItemAt(_loc3_);
      var _loc5_;
      var _loc6_;
      if(_loc4_ != undefined)
      {
         _loc5_ = sExtraData.charAt(0) == "+";
         _loc6_ = sExtraData.charAt(1);
         switch(_loc6_)
         {
            case "H":
               _loc4_.options[dofus.datacenter.Team.OPT_NEED_HELP] = _loc5_;
               break;
            case "S":
               _loc4_.options[dofus.datacenter.Team.OPT_BLOCK_SPECTATOR] = _loc5_;
               break;
            case "A":
               _loc4_.options[dofus.datacenter.Team.OPT_BLOCK_JOINER] = _loc5_;
               break;
            case "P":
               _loc4_.options[dofus.datacenter.Team.OPT_BLOCK_JOINER_EXCEPT_PARTY_MEMBER] = _loc5_;
         }
         this.api.gfx.addSpriteOverHeadItem(_loc3_,"FightOptions",dofus.graphics.battlefield.FightOptionsOverHead,[_loc4_],undefined);
      }
   }
   function onLeave()
   {
      this.api.datacenter.Game.currentPlayerID = undefined;
      this.api.ui.getUIComponent("Banner").hideRightPanel(true);
      this.api.ui.unloadUIComponent("Timeline");
      this.api.ui.unloadUIComponent("StringCourse");
      this.api.ui.unloadUIComponent("PlayerInfos");
      this.api.ui.unloadUIComponent("SpriteInfos");
      this.aks.GameActions.onActionsFinish(String(this.api.datacenter.Player.ID));
      this.api.datacenter.Player.reset();
      this.api.datacenter.Player.isDead = false;
      var _loc2_ = dofus.graphics.gapi.ui.FightChallenge(dofus.graphics.gapi.ui.FightChallenge(this.api.ui.getUIComponent("FightChallenge")));
      _loc2_.cleanChallenge();
   }
   function onEnd(sExtraData)
   {
      if(this.api.kernel.MapsServersManager.isBuilding)
      {
         this.addToQueue({object:this,method:this.onEnd,params:[sExtraData]});
         return undefined;
      }
      this.aks.Game.isBusy = true;
      this.aks.Game.create();
      var _loc4_ = dofus.graphics.gapi.ui.FightChallenge(dofus.graphics.gapi.ui.FightChallenge(this.api.ui.getUIComponent("FightChallenge")));
      this.api.kernel.StreamingDisplayManager.onFightEnd();
      var _loc5_ = {winners:[],loosers:[],collectors:[],challenges:_loc4_.challenges.deepClone(),currentTableTurn:this.api.datacenter.Game.currentTableTurn,currentPlayerInfos:[],currentPlayerInfosWithChest:[]};
      this.api.datacenter.Game.results = _loc5_;
      if(!this.api.datacenter.Game.isSpectator)
      {
         this.api.datacenter.Basics.currentSessionFightCount++;
         _loc5_.id = this.api.datacenter.Basics.currentSessionFightCount;
         this.api.datacenter.Game.storeFightResults(_loc5_);
      }
      _loc4_.cleanChallenge();
      var _loc6_ = sExtraData.split("|");
      var _loc7_ = -1;
      var _loc8_;
      if(!_global.isNaN(Number(_loc6_[0])))
      {
         _loc5_.duration = Number(_loc6_[0]);
      }
      else
      {
         _loc8_ = _loc6_[0].split(";");
         _loc5_.duration = Number(_loc8_[0]);
         _loc7_ = Number(_loc8_[1]);
      }
      this.api.datacenter.Basics.aks_game_end_bonus = _loc7_;
      var _loc9_ = Number(_loc6_[1]);
      var _loc10_ = Number(_loc6_[2]);
      _loc5_.fightType = _loc10_;
      var _loc11_ = new ank.utils.ExtendedArray();
      var _loc12_ = 0;
      this.api.datacenter.Player.isDead = false;
      this.parsePlayerData(_loc5_,3,_loc9_,_loc6_,_loc10_,_loc12_,_loc11_,false,false);
   }
   function parsePlayerData(oResults, nStartIndex, nSenderID, aTmp, nFightType, nKamaDrop, eaFightDrop, bAlreadyParsed, bIsChest)
   {
      var _loc11_ = nStartIndex;
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
      while(_loc11_ < aTmp.length)
      {
         _loc12_ = aTmp[_loc11_].split(";");
         _loc13_ = {};
         if(Number(_loc12_[0]) != 6)
         {
            _loc13_.id = Number(_loc12_[1]);
            if(_loc13_.id == this.api.datacenter.Player.ID)
            {
               if(Number(_loc12_[0]) == 0)
               {
                  this.api.kernel.SpeakingItemsManager.triggerEvent(dofus.managers.SpeakingItemsManager.SPEAK_TRIGGER_FIGHT_LOST);
               }
               else
               {
                  this.api.kernel.SpeakingItemsManager.triggerEvent(dofus.managers.SpeakingItemsManager.SPEAK_TRIGGER_FIGHT_WON);
               }
            }
            _loc14_ = this.api.kernel.CharactersManager.getNameFromData(_loc12_[2]);
            _loc13_.name = _loc14_.name;
            _loc13_.type = _loc14_.type;
            _loc13_.level = Number(_loc12_[3]);
            _loc13_.bDead = _loc12_[5] != "1" ? false : true;
            _loc13_.gfx = Number(_loc12_[4]);
            switch(nFightType)
            {
               case 0:
                  _loc13_.minxp = Number(_loc12_[6]);
                  _loc13_.xp = Number(_loc12_[7]);
                  _loc13_.maxxp = Number(_loc12_[8]);
                  _loc13_.winxp = Math.max(Number(_loc12_[9]),0);
                  _loc13_.guildxp = Number(_loc12_[10]);
                  _loc13_.mountxp = Number(_loc12_[11]);
                  _loc15_ = _loc12_[12].split(",");
                  if(_loc13_.id == this.api.datacenter.Player.ID && _loc15_.length > 10)
                  {
                     this.api.kernel.SpeakingItemsManager.triggerEvent(dofus.managers.SpeakingItemsManager.SPEAK_TRIGGER_GREAT_DROP);
                  }
                  _loc13_.kama = _loc12_[13];
                  break;
               case 1:
                  _loc13_.minhonour = Number(_loc12_[6]);
                  _loc13_.honour = Number(_loc12_[7]);
                  _loc13_.maxhonour = Number(_loc12_[8]);
                  _loc13_.winhonour = Number(_loc12_[9]);
                  _loc13_.rank = Number(_loc12_[10]);
                  _loc13_.disgrace = Number(_loc12_[11]);
                  _loc13_.windisgrace = Number(_loc12_[12]);
                  _loc13_.maxdisgrace = this.api.lang.getMaxDisgracePoints();
                  _loc13_.mindisgrace = 0;
                  _loc13_.alignment = Number(_loc12_[13]);
                  _loc15_ = _loc12_[14].split(",");
                  if(_loc13_.id == this.api.datacenter.Player.ID && _loc15_.length > 10)
                  {
                     this.api.kernel.SpeakingItemsManager.triggerEvent(dofus.managers.SpeakingItemsManager.SPEAK_TRIGGER_GREAT_DROP);
                  }
                  _loc13_.kama = _loc12_[15];
                  _loc13_.minxp = Number(_loc12_[16]);
                  _loc13_.xp = Number(_loc12_[17]);
                  _loc13_.maxxp = Number(_loc12_[18]);
                  _loc13_.winxp = Number(_loc12_[19]);
            }
         }
         else
         {
            _loc15_ = _loc12_[1].split(",");
            _loc13_.kama = _loc12_[2];
            nKamaDrop += Number(_loc13_.kama);
         }
         _loc13_.items = [];
         _loc13_.items = this.parseItems(_loc15_);
         switch(Number(_loc12_[0]))
         {
            case 0:
               oResults.loosers.push(_loc13_);
               break;
            case 2:
               oResults.winners.push(_loc13_);
               break;
            case 5:
               oResults.collectors.push(_loc13_);
               break;
            case 6:
               eaFightDrop = eaFightDrop.concat(_loc13_.items);
         }
         if(!bAlreadyParsed && (_loc13_.id == this.api.datacenter.Player.ID || bIsChest))
         {
            if(bIsChest)
            {
               _loc16_ = new ank.utils.ExtendedObject();
               _loc17_ = [];
               _loc18_ = oResults.currentPlayerInfos[0].items;
               _loc19_ = 0;
               while(_loc19_ < _loc18_.length)
               {
                  _loc20_ = _loc18_[_loc19_];
                  _loc21_ = new dofus.datacenter.Item(undefined,_loc20_.unicID,_loc20_.Quantity);
                  _loc17_.push(_loc21_);
                  _loc16_.addItemAt(_loc20_.unicID,_loc21_);
                  _loc19_ += 1;
               }
               _loc22_ = _loc13_.items;
               _loc23_ = 0;
               while(_loc23_ < _loc22_.length)
               {
                  _loc24_ = _loc22_[_loc23_];
                  if(_loc16_.getItemAt(_loc24_.unicID) != undefined)
                  {
                     _loc25_ = dofus.datacenter.Item(_loc16_.getItemAt(_loc24_.unicID));
                     _loc25_.Quantity += _loc24_.Quantity;
                  }
                  else
                  {
                     _loc17_.push(_loc24_);
                  }
                  _loc23_ += 1;
               }
               this.api.datacenter.Basics.kamas_lastGained = Number(this.api.datacenter.Basics.kamas_lastGained) + Number(_loc12_[13]);
               _loc26_ = {};
               _loc26_.type = oResults.currentPlayerInfos[0].type;
               _loc26_.winxp = this.api.datacenter.Basics.exp_lastGained;
               _loc26_.guildxp = this.api.datacenter.Basics.guildExp_lastGained;
               _loc26_.mountxp = this.api.datacenter.Basics.mountExp_lastGained;
               _loc26_.kama = this.api.datacenter.Basics.kamas_lastGained;
               _loc26_.items = _loc17_;
               oResults.currentPlayerInfosWithChest.push(_loc26_);
               bAlreadyParsed = true;
            }
            else
            {
               if(this.api.datacenter.Player.Guild == 3 && nFightType == 0)
               {
                  if(aTmp[_loc11_ + 1].split(";")[2] == 285)
                  {
                     bIsChest = true;
                  }
                  else
                  {
                     bAlreadyParsed = true;
                  }
               }
               else
               {
                  bAlreadyParsed = true;
               }
               this.api.datacenter.Basics.exp_lastGained = _loc13_.winxp;
               this.api.datacenter.Basics.kamas_lastGained = _loc13_.kama;
               this.api.datacenter.Basics.guildExp_lastGained = _loc13_.guildxp;
               this.api.datacenter.Basics.mountExp_lastGained = _loc13_.mountxp;
               oResults.currentPlayerInfos.push(_loc13_);
            }
         }
         _loc11_ += 1;
      }
      this.onParseItemEnd(nSenderID,oResults,eaFightDrop,nKamaDrop);
   }
   function parseItems(aItems)
   {
      var _loc3_ = [];
      var _loc4_ = 0;
      var _loc5_;
      var _loc6_;
      var _loc7_;
      var _loc8_;
      var _loc9_;
      while(_loc4_ < aItems.length)
      {
         _loc5_ = aItems[_loc4_].split("~");
         _loc6_ = Number(_loc5_[0]);
         _loc7_ = Number(_loc5_[1]);
         _loc8_ = _loc5_[2] != undefined ? Number(_loc5_[2]) : 0;
         if(_global.isNaN(_loc6_))
         {
            break;
         }
         if(_loc6_ != 0)
         {
            _loc9_ = new dofus.datacenter.Item(_loc8_,_loc6_,_loc7_);
            _loc9_._nRealUnicId = _loc8_;
            _loc3_.push(_loc9_);
         }
         _loc4_ += 1;
      }
      return _loc3_;
   }
   function onParseItemEnd(nSenderID, oResults, eaFightDrop, nKamaDrop)
   {
      var _loc6_;
      var _loc7_;
      var _loc8_;
      var _loc9_;
      if(eaFightDrop.length)
      {
         _loc6_ = Math.ceil(eaFightDrop.length / oResults.winners.length);
         _loc7_ = 0;
         while(_loc7_ < oResults.winners.length)
         {
            _loc8_ = eaFightDrop.length;
            oResults.winners[_loc7_].kama = Math.ceil(nKamaDrop / _loc6_);
            if(_loc7_ == oResults.winners.length - 1)
            {
               _loc6_ = _loc8_;
            }
            _loc9_ = _loc8_ - _loc6_;
            while(_loc9_ < _loc8_)
            {
               oResults.winners[_loc7_].items.push(eaFightDrop.pop());
               _loc9_ += 1;
            }
            _loc7_ += 1;
         }
      }
      if(nSenderID == this.api.datacenter.Player.ID)
      {
         this.aks.GameActions.onActionsFinish(String(nSenderID));
      }
      this.api.datacenter.Game.isRunning = false;
      var _loc10_;
      var _loc11_ = this.api.datacenter.Game.currentPlayerID;
      var _loc12_;
      if(_loc11_ != undefined)
      {
         _loc12_ = this.api.datacenter.Sprites.getItemAt(_loc11_);
         if(_loc12_ != undefined)
         {
            _loc10_ = _loc12_.sequencer;
         }
      }
      var _loc13_;
      if(_loc10_ == undefined)
      {
         _loc13_ = this.api.datacenter.Sprites.getItemAt(nSenderID);
         if(_loc13_ != undefined)
         {
            _loc10_ = _loc13_.sequencer;
         }
      }
      var _loc14_;
      if(_loc10_ == undefined)
      {
         _loc14_ = this.api.datacenter.Player.data;
         if(_loc14_ != undefined)
         {
            _loc10_ = _loc14_.sequencer;
         }
      }
      this.aks.Game.isBusy = false;
      if(_loc10_ != undefined)
      {
         _loc10_.clear();
      }
      this.api.kernel.GameManager.terminateFight();
      this.api.kernel.TipsManager.showNewTip(dofus.managers.TipsManager.TIP_FIGHT_ENDFIGHT);
   }
   function onExtraClip(sExtraData)
   {
      var _loc3_ = sExtraData.split("|");
      var _loc4_ = _loc3_[0];
      var _loc5_ = _loc3_[1].split(";");
      var _loc6_ = dofus.Constants.EXTRA_PATH + _loc4_ + ".swf";
      var _loc7_ = _loc4_ == "-";
      var _loc8_;
      for(var _loc9_ in _loc5_)
      {
         _loc8_ = _loc5_[_loc9_];
         if(_loc7_)
         {
            this.api.gfx.removeSpriteExtraClip(_loc8_,false);
         }
         else
         {
            this.api.gfx.addSpriteExtraClip(_loc8_,_loc6_,undefined,false);
         }
      }
   }
   function onGameOver()
   {
      this.api.network.softDisconnect();
      this.api.ui.loadUIComponent("GameOver","GameOver",undefined,{bAlwaysOnTop:true});
   }
   function _hardAura_getTemplateIds(oSprite)
   {
      if(oSprite != undefined && oSprite._aTemplateIds != undefined)
      {
         return oSprite._aTemplateIds;
      }
      if(_global._hardGlowTemplateIds != undefined && oSprite != undefined)
      {
         return _global._hardGlowTemplateIds[String(oSprite.id)];
      }
      return undefined;
   }
   function _hardAura_firstTemplateId(xTemplateIds)
   {
      if(xTemplateIds == undefined)
      {
         return undefined;
      }
      if(xTemplateIds instanceof Array)
      {
         return Number(xTemplateIds[0]);
      }
      if(typeof xTemplateIds == "string")
      {
         if(xTemplateIds.indexOf(",") != -1)
         {
            return Number(xTemplateIds.split(",")[0]);
         }
         return Number(xTemplateIds);
      }
      return Number(xTemplateIds);
   }
   function _hardAura_computeColor(nTemplateId)
   {
      if(_global.isNaN(nTemplateId))
      {
         return null;
      }
      if(nTemplateId <= 100000)
      {
         return null;
      }
      if(nTemplateId <= 200000)
      {
         return 0;
      }
      if(nTemplateId <= 300000)
      {
         return 8388863;
      }
      return 16746496;
   }
   function _hardAura_debugLog(sMessage)
   {
      if(!dofus.Constants.DEBUG_MOB_FILTERS)
      {
         return undefined;
      }
      ank.utils.Logger.log("[MobFilter] " + sMessage);
   }
   function _hardAura_getGfxClip(nSpriteId)
   {
      var _loc3_;
      if(this.api != undefined && this.api.gfx != undefined)
      {
         if(this.api.gfx.getSprite != undefined)
         {
            _loc3_ = this.api.gfx.getSprite(nSpriteId);
         }
      }
      if(_loc3_ == undefined && this.api != undefined && this.api.gfx != undefined && this.api.gfx.spriteHandler != undefined)
      {
         _loc3_ = this.api.gfx.spriteHandler.getSprites().getItemAt(nSpriteId);
      }
      if(_loc3_ == undefined)
      {
         return undefined;
      }
      var _loc4_ = _loc3_.mc != undefined ? _loc3_.mc : _loc3_;
      if(_loc4_ == undefined)
      {
         return undefined;
      }
      if(_loc4_._mcGfx != undefined && _loc4_._mcGfx._width > 0)
      {
         return _loc4_._mcGfx;
      }
      if(_loc4_._mcAnim != undefined)
      {
         return _loc4_._mcAnim;
      }
      if(_loc4_._mcFighter != undefined)
      {
         return _loc4_._mcFighter;
      }
      if(_loc4_._mcBody != undefined)
      {
         return _loc4_._mcBody;
      }
      if(_loc4_._mcGfx != undefined)
      {
         return _loc4_._mcGfx;
      }
      return _loc4_;
   }
   function _hardAura_applyFilter(nSpriteId, nColor, sReason)
   {
      var _loc5_ = this._hardAura_getGfxClip(nSpriteId);
      if(_loc5_ == undefined)
      {
         this._hardAura_debugLog("applyFilter FAILED for " + nSpriteId + " - no gfx clip (" + sReason + ")");
         return false;
      }
      _loc5_.cacheAsBitmap = true;
      if(_loc5_._parent != undefined)
      {
         _loc5_._parent.cacheAsBitmap = true;
      }
      var _loc6_ = 1;
      var _loc7_ = 8;
      var _loc8_ = 3;
      var _loc9_ = 2;
      var _loc10_ = new flash.filters.GlowFilter(nColor,_loc6_,_loc7_,_loc7_,_loc8_,_loc9_,false,false);
      _loc5_.filters = [_loc10_];
      var _loc11_ = this.api.datacenter.Sprites.getItemAt(nSpriteId);
      if(_loc11_ != undefined)
      {
         _loc11_._hardAuraColor = nColor;
         _loc11_._hardAuraClipRef = _loc5_;
      }
      this._hardAura_debugLog("applyFilter OK for " + nSpriteId + " color=" + nColor + " (" + sReason + ")");
      this._hardAura_scheduleVerify(nSpriteId,nColor,sReason);
      return true;
   }
   function _hardAura_scheduleVerify(nSpriteId, nColor, sReason)
   {
      if(this._hardAuraVerifyIntervals == undefined)
      {
         this._hardAuraVerifyIntervals = {};
      }
      if(this._hardAuraVerifyRetries == undefined)
      {
         this._hardAuraVerifyRetries = {};
      }
      if(this._hardAuraVerifyIntervals[nSpriteId] != undefined)
      {
         _global.clearInterval(this._hardAuraVerifyIntervals[nSpriteId]);
      }
      this._hardAuraVerifyRetries[nSpriteId] = 0;
      this._hardAuraVerifyIntervals[nSpriteId] = _global.setInterval(this,"_hardAura_tickVerify",150,nSpriteId,nColor,sReason);
   }
   function _hardAura_tickVerify(nSpriteId, nColor, sReason)
   {
      if(this._hardAuraVerifyRetries == undefined || this._hardAuraVerifyIntervals == undefined)
      {
         return undefined;
      }
      if(this.api.datacenter.Game.isFight)
      {
         _global.clearInterval(this._hardAuraVerifyIntervals[nSpriteId]);
         delete this._hardAuraVerifyIntervals[nSpriteId];
         delete this._hardAuraVerifyRetries[nSpriteId];
         return undefined;
      }
      this._hardAuraVerifyRetries[nSpriteId]++;
      if(this._hardAuraVerifyRetries[nSpriteId] > 60)
      {
         this._hardAura_debugLog("tickVerify GIVEUP for " + nSpriteId + " after 60 retries");
         _global.clearInterval(this._hardAuraVerifyIntervals[nSpriteId]);
         delete this._hardAuraVerifyIntervals[nSpriteId];
         delete this._hardAuraVerifyRetries[nSpriteId];
         return undefined;
      }
      var _loc6_ = this._hardAura_getGfxClip(nSpriteId);
      if(_loc6_ == undefined)
      {
         return undefined;
      }
      var _loc7_ = this.api.datacenter.Sprites.getItemAt(nSpriteId);
      var _loc8_ = _loc7_ != undefined && _loc7_._hardAuraClipRef != _loc6_;
      var _loc9_ = _loc6_.filters == undefined || _loc6_.filters.length == 0;
      var _loc10_;
      var _loc11_;
      var _loc12_;
      var _loc13_;
      if(_loc9_ || _loc8_)
      {
         _loc6_.cacheAsBitmap = true;
         if(_loc6_._parent != undefined)
         {
            _loc6_._parent.cacheAsBitmap = true;
         }
         _loc10_ = 1;
         _loc11_ = 8;
         _loc12_ = 3;
         _loc13_ = 2;
         _loc6_.filters = [new flash.filters.GlowFilter(nColor,_loc10_,_loc11_,_loc11_,_loc12_,_loc13_,false,false)];
         if(_loc7_ != undefined)
         {
            _loc7_._hardAuraClipRef = _loc6_;
         }
         this._hardAura_debugLog("tickVerify REAPPLIED for " + nSpriteId + " (clipChanged=" + _loc8_ + ", filtersEmpty=" + _loc9_ + ")");
      }
   }
   function _hardAura_clearFilter(nSpriteId, sReason)
   {
      var _loc5_ = this._hardAura_getGfxClip(nSpriteId);
      if(_loc5_ != undefined)
      {
         _loc5_.filters = [];
      }
      var _loc6_ = this.api.datacenter.Sprites.getItemAt(nSpriteId);
      if(_loc6_ != undefined)
      {
         delete _loc6_._hardAuraColor;
         delete _loc6_._hardAuraClipRef;
      }
      if(this._hardAuraVerifyIntervals != undefined && this._hardAuraVerifyIntervals[nSpriteId] != undefined)
      {
         _global.clearInterval(this._hardAuraVerifyIntervals[nSpriteId]);
         delete this._hardAuraVerifyIntervals[nSpriteId];
         delete this._hardAuraVerifyRetries[nSpriteId];
      }
      this._hardAura_debugLog("clearFilter for " + nSpriteId + " (" + sReason + ")");
   }
   function _hardAura_clearScheduler(nSpriteId, sReason)
   {
      var _loc5_ = String(nSpriteId);
      if(this._hardAuraIntervals[_loc5_] != undefined)
      {
         _global.clearInterval(this._hardAuraIntervals[_loc5_]);
         delete this._hardAuraIntervals[_loc5_];
      }
      delete this._hardAuraRetries[_loc5_];
   }
   function _hardAura_applyNow(oSprite, sReason)
   {
      if(this.api.datacenter.Game.isFight)
      {
         this._hardAura_debugLog("applyNow SKIP for " + oSprite.id + " - in fight");
         return undefined;
      }
      if(!this.api.kernel.OptionsManager.getOption("Aura"))
      {
         return undefined;
      }
      var _loc4_ = this._hardAura_getTemplateIds(oSprite);
      var _loc5_ = this._hardAura_firstTemplateId(_loc4_);
      var _loc6_ = this._hardAura_computeColor(_loc5_);
      var _loc7_ = String(oSprite.id);
      if(_loc6_ == null)
      {
         if(this._hardAuraApplied[_loc7_] != undefined)
         {
            this._hardAura_clearFilter(oSprite.id,"NOT_ELIGIBLE");
            delete this._hardAuraApplied[_loc7_];
         }
         return undefined;
      }
      this._hardAura_debugLog("applyNow for " + oSprite.id + " templateId=" + _loc5_ + " color=" + _loc6_ + " (" + sReason + ")");
      if(this._hardAura_applyFilter(oSprite.id,_loc6_,sReason))
      {
         this._hardAuraApplied[_loc7_] = _loc6_;
      }
      else
      {
         this._hardAura_schedule(oSprite,"MC_NOT_READY");
      }
   }
   function reapplyOutOfFightMobFilter(nSpriteId)
   {
      if(this.api.datacenter.Game.isFight)
      {
         return undefined;
      }
      var _loc3_ = this.api.datacenter.Sprites.getItemAt(nSpriteId);
      if(_loc3_ == undefined)
      {
         return undefined;
      }
      if(_loc3_._hardAuraColor != undefined)
      {
         this._hardAura_debugLog("reapplyOutOfFightMobFilter for " + nSpriteId + " color=" + _loc3_._hardAuraColor);
         this._hardAura_applyFilter(nSpriteId,_loc3_._hardAuraColor,"GFX_RELOADED");
      }
      else
      {
         this._hardAura_applyNow(_loc3_,"GFX_RELOADED_NEW");
      }
      var _loc4_;
      var _loc5_;
      if(_loc3_.linkedChilds != undefined)
      {
         _loc4_ = _loc3_.linkedChilds.getItems();
         for(var _loc6_ in _loc4_)
         {
            _loc5_ = _loc4_[_loc6_];
            if(_loc5_ != undefined)
            {
               this._hardAura_applyNow(_loc5_,"GFX_RELOADED_CHILD");
            }
         }
      }
   }
   function reapplyAllOutOfFightMobFilters()
   {
      if(this.api.datacenter.Game.isFight)
      {
         return undefined;
      }
      this._hardAura_debugLog("reapplyAllOutOfFightMobFilters START");
      var _loc2_ = this.api.datacenter.Sprites.getItems();
      var _loc3_;
      for(var _loc4_ in _loc2_)
      {
         _loc3_ = _loc2_[_loc4_];
         if(_loc3_ instanceof dofus.datacenter.MonsterGroup)
         {
            this._hardAura_applyNow(_loc3_,"MAP_REFRESH");
         }
      }
   }
   function _hardAura_schedule(oSprite, sReason)
   {
      var k = String(oSprite.id);
      if(this._hardAuraRetries[k] == undefined)
      {
         this._hardAuraRetries[k] = 0;
      }
      if(this._hardAuraIntervals[k] != undefined)
      {
         return undefined;
      }
      var self = this;
      this._hardAuraIntervals[k] = _global.setInterval(function()
      {
         var _loc1_ = self.api.datacenter.Sprites.getItemAt(oSprite.id);
         self._hardAuraRetries[k] += 1;
         if(_loc1_ == undefined)
         {
            self._hardAura_clearScheduler(oSprite.id,"SPRITE_MISSING");
            if(self._hardAuraApplied[k] != undefined)
            {
               self._hardAura_clearFilter(oSprite.id,"SPRITE_MISSING");
               delete self._hardAuraApplied[k];
            }
            return undefined;
         }
         var _loc2_ = self._hardAura_getTemplateIds(_loc1_);
         if(_loc2_ == undefined)
         {
            if(self._hardAuraRetries[k] >= 12)
            {
               self._hardAura_clearScheduler(oSprite.id,"GIVEUP_NO_TEMPLATEIDS");
            }
            return undefined;
         }
         self._hardAura_clearScheduler(oSprite.id,"TEMPLATEIDS_READY");
         self._hardAura_applyNow(_loc1_,"RETRY_APPLY");
      }
      ,120);
   }
   function onSpriteMovement(bAdd, oSprite, aEffect)
   {
      var _loc6_;
      if(dofus.Constants.DEBUG_ACTIF)
      {
         _loc6_ = oSprite != undefined ? oSprite.id : "undefined";
         if(oSprite != undefined)
         {
         }
      }
      if(_global._hardGlowTemplateIds == undefined)
      {
         _global._hardGlowTemplateIds = {};
      }
      var _loc7_;
      if(oSprite != undefined && oSprite._aTemplateIds != undefined)
      {
         _loc7_ = String(oSprite.id);
         _global._hardGlowTemplateIds[_loc7_] = oSprite._aTemplateIds;
         if(dofus.Constants.DEBUG_ACTIF)
         {
         }
         if(_loc7_.indexOf("_") != -1)
         {
            _global._hardGlowTemplateIds[String(_loc7_.split("_")[0])] = oSprite._aTemplateIds;
            if(dofus.Constants.DEBUG_ACTIF)
            {
            }
         }
      }
      if(oSprite instanceof dofus.datacenter.Character)
      {
         this.api.datacenter.Game.playerCount += !bAdd ? -1 : 1;
      }
      var _loc8_ = oSprite.id;
      var _loc9_;
      var _loc10_;
      var _loc11_;
      if(bAdd)
      {
         if(aEffect != undefined)
         {
            this.api.gfx.spriteLaunchVisualEffect.apply(this.api.gfx,aEffect);
         }
         this.api.gfx.addSprite(_loc8_);
         if(!_global.isNaN(oSprite.scaleX))
         {
            this.api.gfx.setSpriteScale(_loc8_,oSprite.scaleX,oSprite.scaleY);
         }
         if(oSprite instanceof dofus.datacenter.OfflineCharacter)
         {
            oSprite.mc.addExtraClip(dofus.Constants.EXTRA_PATH + oSprite.offlineType + ".swf",undefined,true);
            return undefined;
         }
         if(oSprite instanceof dofus.datacenter.NonPlayableCharacter)
         {
            if(!_global.isNaN(oSprite.extraClipID))
            {
               this.api.gfx.addSpriteExtraClip(_loc8_,dofus.Constants.EXTRA_PATH + oSprite.extraClipID + ".swf",undefined,false);
               return undefined;
            }
         }
         if(this.api.datacenter.Game.isRunning)
         {
            this.api.gfx.addSpriteExtraClip(_loc8_,dofus.Constants.CIRCLE_FILE,dofus.Constants.TEAMS_COLOR[oSprite.Team]);
         }
         else if(oSprite.Aura != 0 && (oSprite.Aura != undefined && this.api.kernel.OptionsManager.getOption("Aura")))
         {
            this.api.gfx.addSpriteExtraClip(_loc8_,dofus.Constants.AURA_PATH + oSprite.Aura + ".swf",undefined,true);
         }
         if(oSprite != undefined)
         {
            if(this._hardAura_getTemplateIds(oSprite) != undefined)
            {
               this._hardAura_applyNow(oSprite,"ON_SPRITE_MOVEMENT_ADD");
            }
            else
            {
               this._hardAura_schedule(oSprite,"ON_SPRITE_MOVEMENT_ADD");
            }
         }
         if(_loc8_ == this.api.datacenter.Player.ID)
         {
            this.api.datacenter.Player.data = oSprite;
            this.api.ui.getUIComponent("Banner").updateLocalPlayer();
         }
         else if(this.api.gfx.spriteHandler.isPlayerSpritesHidden && (oSprite instanceof dofus.datacenter.Character || (oSprite instanceof dofus.datacenter.PlayerShop || oSprite instanceof dofus.datacenter.MonsterGroup)))
         {
            this.api.gfx.spriteHandler.hideSprite(_loc8_,true);
         }
         else if(this.api.gfx.spriteHandler.isShowingMonstersTooltip && oSprite instanceof dofus.datacenter.MonsterGroup)
         {
            oSprite.mc._rollOver(true);
         }
      }
      else if(!this.api.datacenter.Game.isRunning)
      {
         if(oSprite != undefined)
         {
            _loc9_ = String(oSprite.id);
            this._hardAura_clearScheduler(oSprite.id,"SPRITE_REMOVE");
            if(this._hardAuraApplied[_loc9_] != undefined)
            {
               this._hardAura_clearFilter(oSprite.id,"SPRITE_REMOVE");
               delete this._hardAuraApplied[_loc9_];
            }
         }
         this.api.gfx.removeSprite(_loc8_);
      }
      else
      {
         if(oSprite != undefined)
         {
            _loc9_ = String(oSprite.id);
            this._hardAura_clearScheduler(oSprite.id,"SPRITE_REMOVE");
            if(this._hardAuraApplied[_loc9_] != undefined)
            {
               this._hardAura_clearFilter(oSprite.id,"SPRITE_REMOVE");
               delete this._hardAuraApplied[_loc9_];
            }
         }
         _loc10_ = oSprite.sequencer;
         _loc11_ = oSprite.mc;
         _loc10_.addAction(27,false,this.api.kernel,this.api.kernel.showMessage,[undefined,this.api.lang.getText("LEAVE_GAME",[oSprite.name]),"INFO_CHAT"]);
         _loc10_.addAction(28,false,this.api.ui.getUIComponent("Timeline"),this.api.ui.getUIComponent("Timeline").hideItem,[_loc8_]);
         _loc10_.addAction(29,true,_loc11_,_loc11_.setAnim,["Die"],1500,true);
         if(oSprite.hasCarriedChild())
         {
            this.api.gfx.uncarriedSprite(oSprite.carriedChild.id,oSprite.cellNum,false,_loc10_);
            _loc10_.addAction(30,false,this.api.gfx,this.api.gfx.addSpriteExtraClip,[oSprite.carriedChild.id,dofus.Constants.CIRCLE_FILE,dofus.Constants.TEAMS_COLOR[oSprite.carriedChild.Team]]);
         }
         _loc10_.addAction(31,false,_loc11_,_loc11_.clear);
         _loc10_.execute();
         if(this.api.datacenter.Game.currentPlayerID == _loc8_)
         {
            this.api.ui.getUIComponent("Banner").stopTimer();
            this.api.ui.getUIComponent("Timeline").stopChrono();
         }
      }
      if(!bAdd)
      {
         this._clearOutOfFightMobGlowState(_loc8_);
      }
      this.api.kernel.GameManager.applyCreatureMode();
   }
   function sliptGfxData(sGfx)
   {
      var _loc2_;
      if(sGfx.indexOf(",") != -1)
      {
         _loc2_ = sGfx.split(",");
         return {shape:"circle",gfx:_loc2_};
      }
      var _loc3_;
      if(sGfx.indexOf(":") != -1)
      {
         _loc3_ = sGfx.split(":");
         return {shape:"line",gfx:_loc3_};
      }
      return {shape:"none",gfx:[sGfx]};
   }
   function splitGfxForScale(sGfxInput, oData)
   {
      var _loc4_ = sGfxInput.split("^");
      var _loc5_ = _loc4_.length != 2 ? sGfxInput : _loc4_[0];
      var _loc6_ = 100;
      var _loc7_ = 100;
      var _loc8_;
      var _loc9_;
      if(_loc4_.length == 2)
      {
         _loc8_ = _loc4_[1];
         if(_global.isNaN(Number(_loc8_)))
         {
            _loc9_ = _loc8_.split("x");
            _loc6_ = _loc9_.length != 2 ? 100 : Number(_loc9_[0]);
            _loc7_ = _loc9_.length != 2 ? 100 : Number(_loc9_[1]);
         }
         else
         {
            _loc6_ = _loc7_ = Number(_loc8_);
         }
      }
      oData.gfxID = _loc5_;
      oData.scaleX = _loc6_;
      oData.scaleY = _loc7_;
   }
   function createTransitionEffect()
   {
      var _loc2_ = new ank.battlefield.datacenter.VisualEffect();
      _loc2_.id = 5;
      _loc2_.file = dofus.Constants.SPELLS_PATH + "transition.swf";
      _loc2_.level = 5;
      _loc2_.params = [];
      _loc2_.bInFrontOfSprite = true;
      _loc2_.bTryToBypassContainerColor = false;
      return _loc2_;
   }
}
