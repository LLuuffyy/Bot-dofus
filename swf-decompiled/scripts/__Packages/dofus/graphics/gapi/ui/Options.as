class dofus.graphics.gapi.ui.Options extends dofus.graphics.gapi.core.DofusAdvancedComponent
{
   var _btnClose;
   var _btnClose2;
   var _btnDefault;
   var _btnTabDisplay;
   var _btnTabGeneral;
   var _btnTabSound;
   var _eaDisplayStyles;
   var _eaFlashQualities;
   var _eaSpellIconsPacks;
   var _eaStylePoints;
   var _lblAudio;
   var _lblDetailLevel;
   var _lblDisplay;
   var _lblGeneral;
   var _lblOptimize;
   var _mcComboBoxPopup;
   var _mcMask;
   var _mcPlacer;
   var _mcTabViewer;
   var _parent;
   var _sCurrentTab;
   var _sbOptions;
   var _target;
   var _winBackground;
   var addToQueue;
   var api;
   var attachMovie;
   var gapi;
   var getNextHighestDepth;
   var unloadThis;
   static var CLASS_NAME = "Options";
   static var SCROLL_BY = 20;
   function Options()
   {
      super();
   }
   function init()
   {
      super.init(false,dofus.graphics.gapi.ui.Options.CLASS_NAME);
      this.api.electron.showMenuBar(true);
      var _loc4_ = System.capabilities.playerType == "StandAlone" && System.capabilities.os.indexOf("Windows") != -1;
      this._eaDisplayStyles = new ank.utils.ExtendedArray();
      var _loc5_ = _root.electron;
      if(_loc5_)
      {
         this._eaDisplayStyles.push({label:this.api.lang.getText("DISPLAYSTYLE_CLASSIC"),style:"normal"});
         this._eaDisplayStyles.push({label:this.api.lang.getText("DISPLAYSTYLE_WIDESCREENCHATPANEL"),style:dofus.managers.OptionsManager.DISPLAY_STYLE_WIDESCREEN_PANELS});
      }
      else
      {
         this._eaDisplayStyles.push({label:this.api.lang.getText("DISPLAYSTYLE_NORMAL"),style:"normal"});
         if(System.capabilities.screenResolutionY > 950 || _loc4_)
         {
            this._eaDisplayStyles.push({label:this.api.lang.getText("DISPLAYSTYLE_MEDIUM" + (!_loc4_ ? "" : "_RES")),style:"medium"});
            this._eaDisplayStyles.push({label:this.api.lang.getText("DISPLAYSTYLE_MAXIMIZED" + (!_loc4_ ? "" : "_RES")),style:"maximized"});
         }
      }
      this._eaFlashQualities = new ank.utils.ExtendedArray();
      this._eaFlashQualities.push({label:this.api.lang.getText("QUALITY_LOW"),quality:"low"});
      this._eaFlashQualities.push({label:this.api.lang.getText("QUALITY_MEDIUM"),quality:"medium"});
      this._eaFlashQualities.push({label:this.api.lang.getText("QUALITY_HIGH"),quality:"high"});
      this._eaSpellIconsPacks = new ank.utils.ExtendedArray();
      this._eaSpellIconsPacks.push({label:this.api.lang.getText("UI_OPTION_SPELLCOLOR_CLASSIC"),frame:dofus.managers.OptionsManager.OPTION_SPELL_PACK_CLASSIC});
      this._eaSpellIconsPacks.push({label:this.api.lang.getText("UI_OPTION_SPELLCOLOR_REMASTERED"),frame:dofus.managers.OptionsManager.OPTION_SPELL_PACK_REMASTERED});
      this._eaSpellIconsPacks.push({label:this.api.lang.getText("UI_OPTION_SPELLCOLOR_CONTRAST"),frame:dofus.managers.OptionsManager.OPTION_SPELL_PACK_CONTRAST});
      this._eaStylePoints = new ank.utils.ExtendedArray();
      this._eaStylePoints.push({label:this.api.lang.getText("PACK_STYLE_POINT_0"),value:0});
      this._eaStylePoints.push({label:this.api.lang.getText("PACK_STYLE_POINT_1"),value:1});
   }
   function callClose()
   {
      this.closeAllList();
      this.unloadThis();
      return true;
   }
   function closeAllList()
   {
      this._mcTabViewer._cbDisplayStyle.closeList();
      this._mcTabViewer._cbDefaultQuality.closeList();
      this._mcTabViewer._cbSpellIconsPack.closeList();
      this._mcTabViewer._cbStylePoints.closeList();
   }
   function destroy()
   {
      this.api.electron.showMenuBar(false);
   }
   function createChildren()
   {
      this.addToQueue({object:this,method:this.initTexts});
      this.addToQueue({object:this,method:this.addListeners});
      this.addToQueue({object:this,method:this.initData});
      this.addToQueue({object:this,method:this.setCurrentTab,params:["General"]});
   }
   function initTexts()
   {
      this._lblGeneral.text = this.api.lang.getText("OPTIONS_GENERAL");
      this._lblDetailLevel.text = this.api.lang.getText("OPTIONS_DETAILLEVEL");
      this._lblAudio.text = this.api.lang.getText("OPTIONS_AUDIO");
      this._lblOptimize.text = this.api.lang.getText("OPTIONS_OPTIMIZE");
      this._lblDisplay.text = this.api.lang.getText("OPTIONS_DISPLAY");
      this._winBackground.title = this.api.lang.getText("OPTIONS");
      this._btnTabGeneral.label = this.api.lang.getText("OPTIONS_GENERAL");
      this._btnTabSound.label = this.api.lang.getText("OPTIONS_AUDIO");
      this._btnTabDisplay.label = this.api.lang.getText("OPTIONS_DISPLAY");
   }
   function initTabTexts()
   {
      this._mcTabViewer._lblMusic.text = this.api.lang.getText("MUSICS");
      this._mcTabViewer._lblSounds.text = this.api.lang.getText("SOUNDS");
      this._mcTabViewer._lblEnvironment.text = this.api.lang.getText("ENVIRONMENT");
      this._btnClose2.label = this.api.lang.getText("CLOSE");
      this._btnDefault.label = this.api.lang.getText("DEFAUT");
      this._mcTabViewer._btnShortcuts.label = this.api.lang.getText("KEYBORD_SHORTCUT");
      this._mcTabViewer._btnClearCache.label = this.api.lang.getText("CLEAR_CACHE");
      this._mcTabViewer._btnResetTips.label = this.api.lang.getText("REINIT_WORD");
      this._mcTabViewer._btnViewSurvey.label = this.api.lang.getText("OPEN_SURVEY");
      this._mcTabViewer._lblTitleMap.text = this.api.lang.getText("MAP");
      this._mcTabViewer._lblTitleFight.text = this.api.lang.getText("FIGHT");
      this._mcTabViewer._lblTitleSecurity.text = this.api.lang.getText("SECURITY_SHORTCUT");
      this._mcTabViewer._lblTitleUI.text = this.api.lang.getText("INTERFACE_WORD");
      this._mcTabViewer._lblTitleMisc.text = this.api.lang.getText("MISC_WORD");
      this._mcTabViewer._lblTitleOptimiz.text = this.api.lang.getText("OPTIONS_OPTIMIZE");
      this._mcTabViewer._lblTitleScreen.text = this.api.lang.getText("OPTION_TITLE_SCREEN");
      this._mcTabViewer._lblGrid.text = this.api.lang.getText("OPTION_GRID");
      this._mcTabViewer._lblNightMode.text = this.api.lang.getText("OPTION_NIGHT_MODE");
      this._mcTabViewer._lblTransparency.text = this.api.lang.getText("OPTION_TRANSPARENCY");
      this._mcTabViewer._lblSpriteInfos.text = this.api.lang.getText("OPTION_SPRITEINFOS");
      this._mcTabViewer._lblSpriteMove.text = this.api.lang.getText("OPTION_SPRITEMOVE");
      this._mcTabViewer._lblMapInfos.text = this.api.lang.getText("OPTION_MAPINFOS");
      this._mcTabViewer._lblAutoHideSmileys.text = this.api.lang.getText("OPTION_AUTOHIDESMILEYS");
      this._mcTabViewer._lblStringCourse.text = this.api.lang.getText("OPTION_STRINGCOURSE");
      this._mcTabViewer._lblColorfulTactic.text = this.api.lang.getText("OPTION_COLORFULTACTIC");
      this._mcTabViewer._lblPointsOverHead.text = this.api.lang.getText("OPTION_POINTSOVERHEAD");
      this._mcTabViewer._lblChatEffects.text = this.api.lang.getText("OPTION_CHATEFFECTS");
      this._mcTabViewer._lblBuff.text = this.api.lang.getText("OPTION_BUFF");
      this._mcTabViewer._lblAdvancedLineOfSight.text = this.api.lang.getText("OPTION_LINEOFSIGHT");
      this._mcTabViewer._lblRemindTurnTime.text = this.api.lang.getText("OPTION_REMINDTURN");
      this._mcTabViewer._lblHideSpellBar.text = this.api.lang.getText("OPTION_SPELLBAR");
      this._mcTabViewer._lblCraftWrongConfirm.text = this.api.lang.getText("OPTION_WRONG_CRAFT_CONFIRM");
      this._mcTabViewer._lblGuildMessageSound.text = this.api.lang.getText("OPTION_GUILDMESSAGESOUND");
      this._mcTabViewer._lblStartTurnSound.text = this.api.lang.getText("OPTION_STARTTURNSOUND");
      this._mcTabViewer._lblBannerShortcuts.text = this.api.lang.getText("OPTION_BANNERSHORTCUTS");
      this._mcTabViewer._lblTipsOnStart.text = this.api.lang.getText("OPTION_TIPSONSTART");
      this._mcTabViewer._lblCreaturesMode.text = this.api.lang.getText("OPTION_CREATURESMODE");
      this._mcTabViewer._lblDisplayStyle.text = this.api.lang.getText("OPTION_DISPLAYSTYLE");
      this._mcTabViewer._lblMovableBar.text = this.api.lang.getText("OPTION_MOVABLEBAR");
      this._mcTabViewer._lblMovableBarSize.text = this.api.lang.getText("OPTION_MOVABLEBARSIZE");
      this._mcTabViewer._lblSpellBar.text = this.api.lang.getText("OPTION_SPELLBAR");
      this._mcTabViewer._lblViewAllMonsterInGroup.text = this.api.lang.getText("OPTION_VIEWALLMONSTERINGROUP");
      this._mcTabViewer._lblCharacterPreview.text = this.api.lang.getText("OPTION_CHARACTERPREVIEW");
      this._mcTabViewer._lblSeeAllSpell.text = this.api.lang.getText("UI_OPTION_SEEALLSPELL");
      this._mcTabViewer._lblAura.text = this.api.lang.getText("OPTION_AURA");
      this._mcTabViewer._lblTutorialTips.text = this.api.lang.getText("OPTION_TUTORIALTIPS");
      this._mcTabViewer._lblCensorshipFilter.text = this.api.lang.getText("OPTION_CENSORSHIP_FILTER");
      this._mcTabViewer._lblDefaultQuality.text = this.api.lang.getText("OPTION_DEFAULTQUALITY");
      this._mcTabViewer._lblSpeakingItems.text = this.api.lang.getText("OPTION_USE_SPEAKINGITEMS");
      this._mcTabViewer._lblConfirmDropItem.text = this.api.lang.getText("OPTION_CONFIRM_DROPITEM");
      this._mcTabViewer._lblChatTimestamp.text = this.api.lang.getText("OPTION_USE_CHATTIMESTAMP");
      this._mcTabViewer._lblViewDicesDammages.text = this.api.lang.getText("OPTION_VIEW_DICES_DAMMAGES");
      this._mcTabViewer._lblAnonymousGameEvents.text = this.api.lang.getText("OPTION_ANONYMOUS_GAME_EVENTS");
      this._mcTabViewer._lblSeeDamagesColor.text = this.api.lang.getText("UI_OPTION_SEEDAMAGESCOLOR");
      this._mcTabViewer._lblRegroupDamage.text = this.api.lang.getText("OPTION_REGROUP_DAMAGE");
      this._mcTabViewer._lblStylePoints.text = this.api.lang.getText("OPTION_POINTS_STYLE");
      this._mcTabViewer._lblViewHPAsBar.text = this.api.lang.getText("OPTION_VIEW_HP_AS_BAR");
      this._mcTabViewer._lblAnimateHPBar.text = this.api.lang.getText("OPTION_ANIMATE_HP_BAR");
      this._mcTabViewer._lblDisableGameResult.text = "Enlever l\'affichage de fin de combat";
      this._mcTabViewer._lblStatesOverHead.text = "Icones d\'etats au-dessus des sprites";
      this._mcTabViewer._lblRemasteredSpellIcons.text = this.api.lang.getText("DOFUS_REMASTERED_SPELL_ICONS");
      if(!this.api.electron.enabled)
      {
         this._mcTabViewer._lblDisplayStyle._visible = false;
         this._mcTabViewer._cbDisplayStyle._visible = false;
      }
   }
   function addListeners()
   {
      this._btnClose.addEventListener("click",this);
      this._btnClose2.addEventListener("click",this);
      this._btnDefault.addEventListener("click",this);
      this._btnTabGeneral.addEventListener("click",this);
      this._btnTabSound.addEventListener("click",this);
      this._btnTabDisplay.addEventListener("click",this);
      this.api.kernel.OptionsManager.addEventListener("optionChanged",this);
      ank.utils.MouseEvents.addListener(this);
   }
   function addTabListeners()
   {
      this._mcTabViewer._btnShortcuts.addEventListener("click",this);
      this._mcTabViewer._btnClearCache.addEventListener("click",this);
      this._mcTabViewer._btnViewSurvey.addEventListener("click",this);
      this._mcTabViewer._btnGrid.addEventListener("click",this);
      this._mcTabViewer._btnNightMode.addEventListener("click",this);
      this._mcTabViewer._btnTransparency.addEventListener("click",this);
      this._mcTabViewer._btnSpriteInfos.addEventListener("click",this);
      this._mcTabViewer._btnSpriteMove.addEventListener("click",this);
      this._mcTabViewer._btnMapInfos.addEventListener("click",this);
      this._mcTabViewer._btnAutoHideSmileys.addEventListener("click",this);
      this._mcTabViewer._btnStringCourse.addEventListener("click",this);
      this._mcTabViewer._btnColorfulTactic.addEventListener("click",this);
      this._mcTabViewer._btnPointsOverHead.addEventListener("click",this);
      this._mcTabViewer._btnChatEffects.addEventListener("click",this);
      this._mcTabViewer._btnBuff.addEventListener("click",this);
      this._mcTabViewer._btnGuildMessageSound.addEventListener("click",this);
      this._mcTabViewer._btnStartTurnSound.addEventListener("click",this);
      this._mcTabViewer._btnBannerShortcuts.addEventListener("click",this);
      this._mcTabViewer._btnTipsOnStart.addEventListener("click",this);
      this._mcTabViewer._btnMovableBar.addEventListener("click",this);
      this._mcTabViewer._btnViewAllMonsterInGroup.addEventListener("click",this);
      this._mcTabViewer._btnCharacterPreview.addEventListener("click",this);
      this._mcTabViewer._btnAura.addEventListener("click",this);
      this._mcTabViewer._btnTutorialTips.addEventListener("click",this);
      this._mcTabViewer._btnResetTips.addEventListener("click",this);
      this._mcTabViewer._btnCensorshipFilter.addEventListener("click",this);
      this._mcTabViewer._btnCraftWrongConfirm.addEventListener("click",this);
      this._mcTabViewer._btnAdvancedLineOfSight.addEventListener("click",this);
      this._mcTabViewer._btnRemindTurnTime.addEventListener("click",this);
      this._mcTabViewer._btnHideSpellBar.addEventListener("click",this);
      this._mcTabViewer._btnSeeAllSpell.addEventListener("click",this);
      this._mcTabViewer._btnSpeakingItems.addEventListener("click",this);
      this._mcTabViewer._btnConfirmDropItem.addEventListener("click",this);
      this._mcTabViewer._btnChatTimestamp.addEventListener("click",this);
      this._mcTabViewer._btnViewDicesDammages.addEventListener("click",this);
      this._mcTabViewer._btnAnonymousGameEvents.addEventListener("click",this);
      this._mcTabViewer._btnSeeDamagesColor.addEventListener("click",this);
      this._mcTabViewer._btnRegroupDamage.addEventListener("click",this);
      this._mcTabViewer._btnViewHPAsBar.addEventListener("click",this);
      this._mcTabViewer._btnAnimateHPBar.addEventListener("click",this);
      this._mcTabViewer._btnDisableGameResult.addEventListener("click",this);
      this._mcTabViewer._btnStatesOverHead.addEventListener("click",this);
      this._mcTabViewer._cbDisplayStyle.addEventListener("itemSelected",this);
      this._mcTabViewer._cbDefaultQuality.addEventListener("itemSelected",this);
      this._mcTabViewer._cbSpellIconsPack.addEventListener("itemSelected",this);
      this._mcTabViewer._cbStylePoints.addEventListener("itemSelected",this);
      this._mcTabViewer._vsMusic.addEventListener("change",this);
      this._mcTabViewer._vsSounds.addEventListener("change",this);
      this._mcTabViewer._vsEnvironment.addEventListener("change",this);
      this._mcTabViewer._vsCreaturesMode.addEventListener("change",this);
      this._mcTabViewer._vsMovableBarSize.addEventListener("change",this);
      this._mcTabViewer._btnMuteMusic.addEventListener("click",this);
      this._mcTabViewer._btnMuteSounds.addEventListener("click",this);
      this._mcTabViewer._btnMuteEnvironment.addEventListener("click",this);
      this._sbOptions.addEventListener("scroll",this);
   }
   function initData()
   {
      this._mcTabViewer._btnShortcuts.enabled = this.api.kernel.XTRA_LANG_FILES_LOADED;
      var _loc3_ = this.api.kernel.OptionsManager;
      this._mcTabViewer._vsMusic.value = _loc3_.getOption("AudioMusicVol");
      this._mcTabViewer._vsSounds.value = _loc3_.getOption("AudioEffectVol");
      this._mcTabViewer._vsEnvironment.value = _loc3_.getOption("AudioEnvVol");
      this._mcTabViewer._btnMuteMusic.selected = _loc3_.getOption("AudioMusicMute");
      this._mcTabViewer._btnMuteSounds.selected = _loc3_.getOption("AudioEffectMute");
      this._mcTabViewer._btnMuteEnvironment.selected = _loc3_.getOption("AudioEnvMute");
      this._mcTabViewer._btnGrid.selected = _loc3_.getOption("Grid");
      this._mcTabViewer._btnNightMode.selected = _loc3_.getOption("NightMode");
      this._mcTabViewer._btnTransparency.selected = _loc3_.getOption("Transparency");
      this._mcTabViewer._btnSpriteInfos.selected = _loc3_.getOption("SpriteInfos");
      this._mcTabViewer._btnSpriteMove.selected = _loc3_.getOption("SpriteMove");
      this._mcTabViewer._btnMapInfos.selected = _loc3_.getOption("MapInfos");
      this._mcTabViewer._btnAutoHideSmileys.selected = _loc3_.getOption("AutoHideSmileys");
      this._mcTabViewer._btnStringCourse.selected = _loc3_.getOption("StringCourse");
      this._mcTabViewer._btnColorfulTactic.selected = _loc3_.getOption("ColorfulTactic");
      this._mcTabViewer._btnPointsOverHead.selected = _loc3_.getOption("PointsOverHead");
      this._mcTabViewer._btnChatEffects.selected = _loc3_.getOption("ChatEffects");
      this._mcTabViewer._btnBuff.selected = _loc3_.getOption("Buff");
      this._mcTabViewer._btnGuildMessageSound.selected = _loc3_.getOption("GuildMessageSound");
      this._mcTabViewer._btnStartTurnSound.selected = _loc3_.getOption("StartTurnSound");
      this._mcTabViewer._btnBannerShortcuts.selected = _loc3_.getOption("BannerShortcuts");
      this._mcTabViewer._btnTipsOnStart.selected = _loc3_.getOption("TipsOnStart");
      this._mcTabViewer._btnViewAllMonsterInGroup.selected = _loc3_.getOption("ViewAllMonsterInGroup");
      this._mcTabViewer._btnCharacterPreview.selected = _loc3_.getOption("CharacterPreview");
      this._mcTabViewer._btnAura.selected = _loc3_.getOption("Aura");
      this._mcTabViewer._btnTutorialTips.selected = _loc3_.getOption("DisplayingFreshTips");
      this._mcTabViewer._btnCensorshipFilter.selected = _loc3_.getOption("CensorshipFilter");
      this._mcTabViewer._btnCraftWrongConfirm.selected = _loc3_.getOption("AskForWrongCraft");
      this._mcTabViewer._btnAdvancedLineOfSight.selected = _loc3_.getOption("AdvancedLineOfSight");
      this._mcTabViewer._btnRemindTurnTime.selected = _loc3_.getOption("RemindTurnTime");
      this._mcTabViewer._btnHideSpellBar.selected = _loc3_.getOption("HideSpellBar");
      this._mcTabViewer._btnSeeAllSpell.selected = !_loc3_.getOption("SeeAllSpell");
      this._mcTabViewer._btnSpeakingItems.selected = _loc3_.getOption("UseSpeakingItems");
      this._mcTabViewer._btnConfirmDropItem.selected = _loc3_.getOption("ConfirmDropItem");
      this._mcTabViewer._btnChatTimestamp.selected = _loc3_.getOption("TimestampInChat");
      this._mcTabViewer._btnViewDicesDammages.selected = _loc3_.getOption("ViewDicesDammages");
      this._mcTabViewer._btnAnonymousGameEvents.selected = _loc3_.getOption("AnonymousGameEvents");
      this._mcTabViewer._btnSeeDamagesColor.selected = _loc3_.getOption("SeeDamagesColor");
      this._mcTabViewer._btnRegroupDamage.selected = _loc3_.getOption("RegroupDamage");
      this._mcTabViewer._btnViewHPAsBar.selected = _loc3_.getOption("ViewHPAsBar");
      this._mcTabViewer._btnAnimateHPBar.selected = _loc3_.getOption("AnimateHPBar");
      this._mcTabViewer._btnDisableGameResult.selected = _loc3_.getOption("DisableGameResult");
      this._mcTabViewer._btnStatesOverHead.selected = _loc3_.getOption("StatesOverHead");
      this._mcTabViewer._btnMovableBar.selected = _loc3_.getOption("MovableBar");
      this._mcTabViewer._vsMovableBarSize.value = _loc3_.getOption("MovableBarSize");
      this._mcTabViewer._lblMovableBarSizeValue.text = _loc3_.getOption("MovableBarSize");
      this._mcTabViewer._vsCreaturesMode.value = _loc3_.getOption("CreaturesMode");
      this._mcTabViewer._lblCreaturesModeValue.text = _global.isFinite(_loc3_.getOption("CreaturesMode")) ? _loc3_.getOption("CreaturesMode") : this.api.lang.getText("INFINIT");
      this._mcTabViewer._cbDefaultQuality.dataProvider = this._eaFlashQualities;
      this._mcTabViewer._cbDefaultQuality.mcListParent = String(this._parent);
      this.selectQuality(_loc3_.getOption("DefaultQuality"));
      this._mcTabViewer._cbSpellIconsPack.dataProvider = this._eaSpellIconsPacks;
      this._mcTabViewer._cbSpellIconsPack.mcListParent = String(this._parent);
      this.selectRemasteredSpellIconsPack(_loc3_.getOption("RemasteredSpellIconsPack"));
      this._mcTabViewer._cbStylePoints.dataProvider = this._eaStylePoints;
      this._mcTabViewer._cbStylePoints.mcListParent = String(this._parent);
      this.selectPointStyle(_loc3_.getOption("StylePoint"));
      this._mcTabViewer._cbDisplayStyle.dataProvider = this._eaDisplayStyles;
      this._mcTabViewer._cbDisplayStyle.mcListParent = String(this._parent);
      var _loc4_ = System.capabilities.playerType == "PlugIn" || (System.capabilities.playerType == "ActiveX" || System.capabilities.playerType == "StandAlone" && System.capabilities.os.indexOf("Windows") != -1);
      this.selectDisplayStyle(_loc4_ ? _loc3_.getOption("DisplayStyle") : "normal");
      this._mcTabViewer._cbDisplayStyle.enabled = _loc4_;
      var _loc5_ = new Color(this._mcTabViewer._cbDisplayStyle);
      _loc5_.setTransform(!_loc4_ ? {ra:30,rb:149,ga:30,gb:145,ba:30,bb:119} : {ra:100,rb:0,ga:100,gb:0,ba:100,bb:0});
   }
   function selectQuality(sQuality)
   {
      var _loc3_ = 0;
      var _loc4_ = 0;
      while(_loc4_ < this._eaFlashQualities.length)
      {
         if(this._eaFlashQualities[_loc4_].quality == sQuality)
         {
            _loc3_ = _loc4_;
            break;
         }
         _loc4_ += 1;
      }
      this._mcTabViewer._cbDefaultQuality.selectedIndex = _loc3_;
   }
   function selectRemasteredSpellIconsPack(nPackFrame)
   {
      var _loc3_ = 0;
      var _loc4_ = 0;
      while(_loc4_ < this._eaSpellIconsPacks.length)
      {
         if(this._eaSpellIconsPacks[_loc4_].frame == nPackFrame)
         {
            _loc3_ = _loc4_;
            break;
         }
         _loc4_ += 1;
      }
      this._mcTabViewer._cbSpellIconsPack.selectedIndex = _loc3_;
   }
   function selectDisplayStyle(sStyleName)
   {
      var _loc3_ = 0;
      var _loc4_ = 0;
      while(_loc4_ < this._eaDisplayStyles.length)
      {
         if(this._eaDisplayStyles[_loc4_].style == sStyleName)
         {
            _loc3_ = _loc4_;
            break;
         }
         _loc4_ += 1;
      }
      this._mcTabViewer._cbDisplayStyle.selectedIndex = _loc3_;
   }
   function selectPointStyle(nIndex)
   {
      this._mcTabViewer._cbStylePoints.selectedIndex = nIndex;
   }
   function updateCurrentTabInformations()
   {
      this._mcTabViewer.removeMovieClip();
      this.attachMovie("Options" + this._sCurrentTab + "Content","_mcTabViewer",this.getNextHighestDepth(),{_x:this._mcPlacer._x,_y:this._mcPlacer._y});
      this._mcTabViewer.setMask(this._mcMask);
      if(this._mcTabViewer._height > this._mcPlacer._height)
      {
         this._sbOptions._visible = true;
         this._sbOptions.min = 0;
         this._sbOptions.max = this._mcTabViewer._height - this._mcPlacer._height;
         this._sbOptions.page = this._sbOptions.max / 2;
      }
      else
      {
         this._sbOptions._visible = false;
      }
      this.addToQueue({object:this,method:this.createDynamicOptions});
      this.addToQueue({object:this,method:this.initData});
      this.addToQueue({object:this,method:this.initTabTexts});
      this.addToQueue({object:this,method:this.addTabListeners});
   }
   function createDynamicOptions()
   {
      if(this._sCurrentTab != "Display")
      {
         return undefined;
      }
      var _loc2_ = this._mcTabViewer._btnAnimateHPBar;
      var _loc3_ = this._mcTabViewer._lblAnimateHPBar;
      if(_loc2_ == undefined)
      {
         return undefined;
      }
      var _loc4_ = _loc2_._y + 17;
      this._mcTabViewer.attachMovie("Button","_btnDisableGameResult",9500,{_bToggle:true,_sBackgroundUp:"ButtonCheckUp",_sBackgroundDown:"ButtonCheckDown",_x:_loc2_._x,_y:_loc4_});
      this._mcTabViewer.attachMovie("Label","_lblDisableGameResult",9501,{_x:_loc3_._x,_y:_loc4_,_width:250,_height:18});
      var _loc5_ = _loc4_ + 17;
      this._mcTabViewer.attachMovie("Button","_btnStatesOverHead",9502,{_bToggle:true,_sBackgroundUp:"ButtonCheckUp",_sBackgroundDown:"ButtonCheckDown",_x:_loc2_._x,_y:_loc5_});
      this._mcTabViewer.attachMovie("Label","_lblStatesOverHead",9503,{_x:_loc3_._x,_y:_loc5_,_width:250,_height:18});
      if(this._mcTabViewer._height > this._mcPlacer._height)
      {
         this._sbOptions._visible = true;
         this._sbOptions.min = 0;
         this._sbOptions.max = this._mcTabViewer._height - this._mcPlacer._height;
         this._sbOptions.page = this._sbOptions.max / 2;
      }
   }
   function setCurrentTab(sNewTab)
   {
      this._mcComboBoxPopup.removeMovieClip();
      var _loc3_ = this["_btnTab" + this._sCurrentTab];
      var _loc4_ = this["_btnTab" + sNewTab];
      _loc3_.selected = true;
      _loc3_.enabled = true;
      _loc4_.selected = false;
      _loc4_.enabled = false;
      this._sCurrentTab = sNewTab;
      this._sbOptions.scrollPosition = 0;
      this.updateCurrentTabInformations();
   }
   function click(oEvent)
   {
      switch(oEvent.target._name)
      {
         case "_btnTabGeneral":
         case "_btnTabSound":
         case "_btnTabDisplay":
            this.closeAllList();
            this.setCurrentTab(oEvent.target._name.substr(7));
            return undefined;
         case "_btnMuteMusic":
            this.api.kernel.OptionsManager.setOption("AudioMusicMute",oEvent.target.selected);
            return undefined;
         case "_btnMuteSounds":
            this.api.kernel.OptionsManager.setOption("AudioEffectMute",oEvent.target.selected);
            return undefined;
         case "_btnMuteEnvironment":
            this.api.kernel.OptionsManager.setOption("AudioEnvMute",oEvent.target.selected);
            return undefined;
         case "_btnClose":
         case "_btnClose2":
            this.callClose();
            return undefined;
         case "_btnDefault":
            this.api.kernel.OptionsManager.loadDefault();
            return undefined;
         case "_btnShortcuts":
            this.api.ui.loadUIComponent("Shortcuts","Shortcuts",undefined,{bAlwaysOnTop:true});
            return undefined;
         case "_btnViewSurvey":
            this.api.network.Survey.getSurvey();
            this.callClose();
            return undefined;
         case "_btnClearCache":
            this.api.kernel.askClearCache();
            return undefined;
         case "_btnGrid":
            this.api.kernel.OptionsManager.setOption("Grid",oEvent.target.selected);
            return undefined;
         case "_btnNightMode":
            this.api.kernel.OptionsManager.setOption("NightMode",oEvent.target.selected);
            return undefined;
         case "_btnTransparency":
            this.api.kernel.OptionsManager.setOption("Transparency",oEvent.target.selected);
            return undefined;
         case "_btnSpriteInfos":
            this.api.kernel.OptionsManager.setOption("SpriteInfos",oEvent.target.selected);
            return undefined;
         case "_btnSpriteMove":
            this.api.kernel.OptionsManager.setOption("SpriteMove",oEvent.target.selected);
            return undefined;
         case "_btnMapInfos":
            this.api.kernel.OptionsManager.setOption("MapInfos",oEvent.target.selected);
            return undefined;
         case "_btnCraftWrongConfirm":
            this.api.kernel.OptionsManager.setOption("AskForWrongCraft",oEvent.target.selected);
            return undefined;
         case "_btnAutoHideSmileys":
            this.api.kernel.OptionsManager.setOption("AutoHideSmileys",oEvent.target.selected);
            return undefined;
         case "_btnStringCourse":
            this.api.kernel.OptionsManager.setOption("StringCourse",oEvent.target.selected);
            return undefined;
         case "_btnColorfulTactic":
            this.api.kernel.OptionsManager.setOption("ColorfulTactic",oEvent.target.selected);
            return undefined;
         case "_btnPointsOverHead":
            this.api.kernel.OptionsManager.setOption("PointsOverHead",oEvent.target.selected);
            return undefined;
         case "_btnChatEffects":
            this.api.kernel.OptionsManager.setOption("ChatEffects",oEvent.target.selected);
            return undefined;
         case "_btnBuff":
            this.api.kernel.OptionsManager.setOption("Buff",oEvent.target.selected);
            return undefined;
         case "_btnGuildMessageSound":
            this.api.kernel.OptionsManager.setOption("GuildMessageSound",oEvent.target.selected);
            return undefined;
         case "_btnStartTurnSound":
            this.api.kernel.OptionsManager.setOption("StartTurnSound",oEvent.target.selected);
            return undefined;
         case "_btnBannerShortcuts":
            this.api.kernel.OptionsManager.setOption("BannerShortcuts",oEvent.target.selected);
            return undefined;
         case "_btnTipsOnStart":
            this.api.kernel.OptionsManager.setOption("TipsOnStart",oEvent.target.selected);
            return undefined;
         case "_btnMovableBar":
            this.api.kernel.OptionsManager.setOption("MovableBar",oEvent.target.selected);
            this.api.kernel.OptionsManager.onMovableBarOptionChanged();
            return undefined;
         case "_btnViewAllMonsterInGroup":
            this.api.kernel.OptionsManager.setOption("ViewAllMonsterInGroup",oEvent.target.selected);
            return undefined;
         case "_btnCharacterPreview":
            this.api.kernel.OptionsManager.setOption("CharacterPreview",oEvent.target.selected);
            return undefined;
         case "_btnAura":
            this.api.kernel.OptionsManager.setOption("Aura",oEvent.target.selected);
            return undefined;
         case "_btnTutorialTips":
            this.api.kernel.OptionsManager.setOption("DisplayingFreshTips",oEvent.target.selected);
            return undefined;
         case "_btnResetTips":
            this.api.kernel.showMessage(undefined,this.api.lang.getText("DO_U_RESET_TIPS"),"CAUTION_YESNO",{name:"ResetTips",listener:this});
            return undefined;
         case "_btnCensorshipFilter":
            this.api.kernel.OptionsManager.setOption("CensorshipFilter",oEvent.target.selected);
            return undefined;
         case "_btnAdvancedLineOfSight":
            this.api.kernel.OptionsManager.setOption("AdvancedLineOfSight",oEvent.target.selected);
            return undefined;
         case "_btnRemindTurnTime":
            this.api.kernel.OptionsManager.setOption("RemindTurnTime",oEvent.target.selected);
            return undefined;
         case "_btnHideSpellBar":
            this.api.kernel.OptionsManager.setOption("HideSpellBar",oEvent.target.selected);
            return undefined;
         case "_btnSeeAllSpell":
            this.api.kernel.OptionsManager.setOption("SeeAllSpell",!oEvent.target.selected);
            return undefined;
         case "_btnSpeakingItems":
            this.api.kernel.OptionsManager.setOption("UseSpeakingItems",oEvent.target.selected);
            return undefined;
         case "_btnConfirmDropItem":
            this.api.kernel.OptionsManager.setOption("ConfirmDropItem",oEvent.target.selected);
            return undefined;
         case "_btnChatTimestamp":
            this.api.kernel.OptionsManager.setOption("TimestampInChat",oEvent.target.selected);
            this.api.kernel.ChatManager.refresh();
            return undefined;
         case "_btnViewDicesDammages":
            this.api.kernel.OptionsManager.setOption("ViewDicesDammages",oEvent.target.selected);
            return undefined;
         case "_btnAnonymousGameEvents":
            this.api.kernel.OptionsManager.setOption("AnonymousGameEvents",oEvent.target.selected);
            return undefined;
         case "_btnSeeDamagesColor":
            this.api.kernel.OptionsManager.setOption("SeeDamagesColor",oEvent.target.selected);
            return undefined;
         case "_btnRegroupDamage":
            this.api.kernel.OptionsManager.setOption("RegroupDamage",oEvent.target.selected);
            return undefined;
         case "_btnViewHPAsBar":
            this.api.kernel.OptionsManager.setOption("ViewHPAsBar",oEvent.target.selected);
            return undefined;
         case "_btnDisableGameResult":
            this.api.kernel.OptionsManager.setOption("DisableGameResult",oEvent.target.selected);
            return undefined;
         case "_btnStatesOverHead":
            this.api.kernel.OptionsManager.setOption("StatesOverHead",oEvent.target.selected);
            return undefined;
         case "_btnAnimateHPBar":
            this.api.kernel.OptionsManager.setOption("AnimateHPBar",oEvent.target.selected);
      }
      return undefined;
   }
   function change(oEvent)
   {
      var _loc3_;
      switch(oEvent.target._name)
      {
         case "_vsMusic":
            this.api.kernel.OptionsManager.setOption("AudioMusicVol",oEvent.target.value);
            return undefined;
         case "_vsSounds":
            this.api.kernel.OptionsManager.setOption("AudioEffectVol",oEvent.target.value);
            return undefined;
         case "_vsEnvironment":
            this.api.kernel.OptionsManager.setOption("AudioEnvVol",oEvent.target.value);
            return undefined;
         case "_vsCreaturesMode":
            if(oEvent.target.value == oEvent.target.max)
            {
               this.api.kernel.OptionsManager.setOption("CreaturesMode",Number.POSITIVE_INFINITY);
               return undefined;
            }
            if(oEvent.target.value == oEvent.target.min)
            {
               this.api.kernel.OptionsManager.setOption("CreaturesMode",0);
               return undefined;
            }
            this.api.kernel.OptionsManager.setOption("CreaturesMode",Math.floor(oEvent.target.value));
            return undefined;
            break;
         case "_vsMovableBarSize":
            _loc3_ = Math.floor(oEvent.target.value);
            this.api.kernel.OptionsManager.setOption("MovableBarSize",_loc3_);
            this._mcTabViewer._lblMovableBarSizeValue.text = _loc3_.toString();
      }
      return undefined;
   }
   function optionChanged(oEvent)
   {
      switch(oEvent.key)
      {
         case "Grid":
            this._mcTabViewer._btnGrid.selected = oEvent.value;
            return undefined;
         case "NightMode":
            this._mcTabViewer._btnNightMode.selected = oEvent.value;
            return undefined;
         case "Transparency":
            this._mcTabViewer._btnTransparency.selected = oEvent.value;
            return undefined;
         case "SpriteInfos":
            this._mcTabViewer._btnSpriteInfos.selected = oEvent.value;
            return undefined;
         case "SpriteMove":
            this._mcTabViewer._btnSpriteMove.selected = oEvent.value;
            return undefined;
         case "MapInfos":
            this._mcTabViewer._btnMapInfos.selected = oEvent.value;
            return undefined;
         case "AutoHideSmileys":
            this._mcTabViewer._btnAutoHideSmileys.selected = oEvent.value;
            return undefined;
         case "StringCourse":
            this._mcTabViewer._btnStringCourse.selected = oEvent.value;
            return undefined;
         case "ColorfulTactic":
            this._mcTabViewer._btnColorfulTactic.selected = oEvent.value;
            return undefined;
         case "PointsOverHead":
            this._mcTabViewer._btnPointsOverHead.selected = oEvent.value;
            return undefined;
         case "ChatEffects":
            this._mcTabViewer._btnChatEffects.selected = oEvent.value;
            return undefined;
         case "CreaturesMode":
            this._mcTabViewer._vsCreaturesMode.value = oEvent.value;
            this._mcTabViewer._lblCreaturesModeValue.text = !_global.isFinite(oEvent.value) ? this.api.lang.getText("INFINIT") : oEvent.value;
            return undefined;
         case "Buff":
            this._mcTabViewer._btnBuff.selected = oEvent.value;
            return undefined;
         case "GuildMessageSound":
            this._mcTabViewer._btnGuildMessageSound.selected = oEvent.value;
            return undefined;
         case "StartTurnSound":
            this._mcTabViewer._btnStartTurnSound.selected = oEvent.value;
            return undefined;
         case "BannerShortcuts":
            this._mcTabViewer._btnBannerShortcuts.selected = oEvent.value;
            return undefined;
         case "TipsOnStart":
            this._mcTabViewer._btnTipsOnStart.selected = oEvent.value;
            return undefined;
         case "DisplayStyle":
            this._mcTabViewer.selectDisplayStyle(oEvent.value);
            return undefined;
         case "MovableBar":
            this._mcTabViewer._btnMovableBar.selected = oEvent.value;
            return undefined;
         case "MovableBarSize":
            this._mcTabViewer._vsMovableBarSize.value = oEvent.value;
            return undefined;
         case "ViewAllMonsterInGroup":
            this._mcTabViewer._btnViewAllMonsterInGroup.selected = oEvent.value;
            return undefined;
         case "CharacterPreview":
            this._mcTabViewer._btnCharacterPreview.selected = oEvent.value;
            return undefined;
         case "Aura":
            this._mcTabViewer._btnAura.selected = oEvent.value;
            return undefined;
         case "DisplayingFreshTips":
            this._mcTabViewer._btnTutorialTips.selected = oEvent.value;
            return undefined;
         case "CensorshipFilter":
            this._mcTabViewer._btnCensorshipFilter.selected = oEvent.value;
            return undefined;
         case "AskForWrongCraft":
            this._mcTabViewer._btnCraftWrongConfirm.selected = oEvent.value;
            return undefined;
         case "AdvancedLineOfSight":
            this._mcTabViewer._btnAdvancedLineOfSight.selected = oEvent.value;
            return undefined;
         case "RemindTurnTime":
            this._mcTabViewer._btnRemindTurnTime.selected = oEvent.value;
            return undefined;
         case "HideSpellBar":
            this._mcTabViewer._btnHideSpellBar.selected = oEvent.value;
            return undefined;
         case "SeeAllSpell":
            this._mcTabViewer._btnSeeAllSpell.selected = !oEvent.value;
            return undefined;
         case "UseSpeakingItems":
            this._mcTabViewer._btnSpeakingItems.selected = oEvent.value;
            return undefined;
         case "ConfirmDropItem":
            this._mcTabViewer._btnConfirmDropItem.selected = oEvent.value;
            return undefined;
         case "TimestampInChat":
            this._mcTabViewer._btnChatTimestamp.selected = oEvent.value;
            this.api.kernel.ChatManager.refresh();
            return undefined;
         case "AudioMusicMute":
            this._mcTabViewer._btnMuteMusic.selected = oEvent.value;
            return undefined;
         case "AudioEffectMute":
            this._mcTabViewer._btnMuteSounds.selected = oEvent.value;
            return undefined;
         case "AudioEnvMute":
            this._mcTabViewer._btnMuteEnvironment.selected = oEvent.value;
            return undefined;
         case "RegroupDamage":
            this._mcTabViewer._btnRegroupDamage.selected = oEvent.value;
            return undefined;
         case "DisableGameResult":
            this._mcTabViewer._btnDisableGameResult.selected = oEvent.value;
            return undefined;
         case "StatesOverHead":
            this._mcTabViewer._btnStatesOverHead.selected = oEvent.value;
      }
      return undefined;
   }
   function itemSelected(oEvent)
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
      switch(oEvent.target._name)
      {
         case "_cbDisplayStyle":
            _loc3_ = oEvent.target.selectedItem;
            if(_loc3_.style == "normal" || this.api.electron.enabled)
            {
               this.api.kernel.OptionsManager.setOption("DisplayStyle",_loc3_.style);
               return undefined;
            }
            this.api.kernel.showMessage(this.api.lang.getText("OPTIONS_DISPLAY"),this.api.lang.getText("DO_U_CHANGE_DISPLAYSTYLE"),"CAUTION_YESNO",{name:"Display",listener:this,params:{style:_loc3_.style}});
            return undefined;
            break;
         case "_cbDefaultQuality":
            _loc4_ = oEvent.target.selectedItem;
            this.api.kernel.showMessage(this.api.lang.getText("OPTIONS_DISPLAY"),this.api.lang.getText("DO_U_CHANGE_QUALITY_" + String(_loc4_.quality).toUpperCase()),"CAUTION_YESNO",{name:"Quality",listener:this,params:{quality:_loc4_.quality}});
            return undefined;
         case "_cbSpellIconsPack":
            _loc5_ = oEvent.target.selectedItem;
            _loc6_ = _loc5_.frame;
            _loc7_ = this.api.kernel.OptionsManager.getOption("RemasteredSpellIconsPack");
            if(_loc7_ != _loc6_)
            {
               this.api.kernel.OptionsManager.setOption("RemasteredSpellIconsPack",_loc6_);
               this.selectRemasteredSpellIconsPack(_loc6_);
               _loc8_ = dofus.graphics.gapi.ui.Banner(this.gapi.getUIComponent("Banner"));
               if(_loc8_ != undefined)
               {
                  _loc8_.shortcuts.updateSpells();
               }
               _loc9_ = dofus.graphics.gapi.ui.Spells(this.gapi.getUIComponent("Spells"));
               if(_loc9_ != undefined)
               {
                  _loc9_.updateSpells();
                  _loc10_ = _loc9_.spellFullInfosViewer;
                  if(_loc10_ != undefined)
                  {
                     _loc10_.updateData();
                  }
               }
               _loc11_ = dofus.graphics.gapi.ui.SpellViewerOnCreate(this.gapi.getUIComponent("SpellViewerOnCreate"));
               if(_loc11_ != undefined)
               {
                  _loc11_.refreshSpellsPack();
               }
               _loc12_ = dofus.graphics.gapi.ui.SpellsCollection(this.gapi.getUIComponent("SpellsCollection"));
               if(_loc12_ != undefined)
               {
                  _loc12_.initData();
                  return undefined;
               }
               return undefined;
            }
            return undefined;
            break;
         case "_cbStylePoints":
            _loc13_ = oEvent.target.selectedItem;
            this.api.kernel.OptionsManager.setOption("StylePoint",_loc13_.value);
      }
      return undefined;
   }
   function yes(oEvent)
   {
      switch(oEvent.target._name)
      {
         case "AskYesNoDisplay":
            this.api.kernel.OptionsManager.setOption("DisplayStyle",oEvent.target.params.style);
            return undefined;
         case "AskYesNoResetTips":
            dofus.managers.TipsManager.getInstance().resetDisplayedTipsList();
            return undefined;
         case "AskYesNoQuality":
            this.api.kernel.OptionsManager.setOption("DefaultQuality",oEvent.target.params.quality);
      }
      return undefined;
   }
   function no(oEvent)
   {
      switch(oEvent.target._name)
      {
         case "AskYesNoDisplay":
            this.selectDisplayStyle(this.api.kernel.OptionsManager.getOption("DisplayStyle"));
            return undefined;
         case "AskYesNoQuality":
            this.selectQuality(this.api.kernel.OptionsManager.getOption("DefaultQuality"));
      }
      return undefined;
   }
   function scroll(oEvent)
   {
      this._mcTabViewer._y = this._mcPlacer._y - this._sbOptions.scrollPosition;
      this.closeAllList();
   }
   function onMouseWheel(nDelta, mc)
   {
      if(dofus.graphics.gapi.ui.Zoom.isZooming())
      {
         return undefined;
      }
      if(String(mc._target).indexOf(this._target) != -1 && this._sbOptions._visible)
      {
         this._sbOptions.scrollPosition -= nDelta <= 0 ? - dofus.graphics.gapi.ui.Options.SCROLL_BY : dofus.graphics.gapi.ui.Options.SCROLL_BY;
      }
   }
}
