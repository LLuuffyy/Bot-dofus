class dofus.DofusLoader extends ank.utils.QueueEmbedMovieClip
{
   var ERRORS;
   var LANG_TEXT;
   var _aCurrentDataBanks;
   var _aCurrentModule;
   var _aCurrentModules;
   var _bBannerDisplay;
   var _bNonCriticalError;
   var _bUpdate;
   var _btnChoose;
   var _btnClearCache;
   var _btnContinue;
   var _btnCopyLogsToClipbard;
   var _btnNext;
   var _btnShowLogs;
   var _cLogger;
   var _cLoggerError;
   var _cLoggerInit;
   var _currentLogger;
   var _lblConfiguration;
   var _lblConnexionServer;
   var _lstConfiguration;
   var _lstConnexionServer;
   var _mcBanner;
   var _mcBannerPlacer;
   var _mcContainer;
   var _mcCurrentModule;
   var _mcLoadingWindow;
   var _mcLocalFileList;
   var _mcModules;
   var _mcProgressBarGroup;
   var _mcTotalProgressBarGroup;
   var _mcWaitBar;
   var _mclLoader;
   var _nLoadedLangFiles;
   var _nOccurenceId;
   var _nRemainingXtraFilesToLoad;
   var _nTotalXtraFilesToLoad;
   var _sPrefixURL;
   var _sStep;
   var _timedProgress;
   var _url;
   var _visible;
   var attachMovie;
   var childNodes;
   var createEmptyMovieClip;
   var firstChild;
   var getNextHighestDepth;
   var getURL;
   var TABULATION = "&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;";
   var _sLogs = "";
   var _sLang = "fr";
   var _bLocalFileListLoaded = false;
   var _bSkipDistantLoad = false;
   var _aCurrentXtraLoadFile = [];
   var _aXtraCurrentVersion = [];
   var _aCurrentXtra = [];
   var _aXtraList = [];
   var _aDistantFilesList = [];
   var _aLoadingBannersFiles = [];
   var _bLoadingBannersFilesLoaded = false;
   var _nProgressIndex = 0;
   var _nTimerJs = 0;
   var _bJsTimer = true;
   function DofusLoader()
   {
      super();
      ank.utils.Extensions.addExtensions();
      this.initLoader(_root);
   }
   static function main(mcRoot)
   {
      var _loc3_ = _global.API;
      var _loc4_;
      if(_loc3_ != undefined)
      {
         _loc4_ = _loc3_.kernel;
         _loc4_.setQuality(_loc4_.OptionsManager.getOption("DefaultQuality"));
      }
      if(_root.dofusPreLoaderMc == undefined)
      {
         return undefined;
      }
      System.security.allowDomain("*");
      fscommand("trapallkeys","true");
      fscommand("CustomerStart","");
      var _loc5_ = _root.electron;
      _root = mcRoot;
      _root.electron = _loc5_;
      dofus.DofusLoader.registerAllClasses();
      _root._quality = "HIGH";
      if(dofus.Constants.TRIPLEFRAMERATE)
      {
         _root.attachMovie("DofusLoader_TripleFramerate","_loader",_root.getNextHighestDepth());
      }
      else
      {
         _root.attachMovie("DofusLoader","_loader",_root.getNextHighestDepth());
      }
      _root.attachMovie("LoaderBorder","_loaderBorder",_root.getNextHighestDepth(),{_x:-3,_y:-2});
      _root.createEmptyMovieClip("_misc",_root.getNextHighestDepth());
   }
   function addLoadingBannersFiles(bShow)
   {
      var xDoc = new XML();
      xDoc.onLoad = function(bSuccess)
      {
         var _loc3_;
         var _loc4_;
         if(bSuccess)
         {
            _loc3_ = this.firstChild.firstChild;
            if(_loc3_ != null && this.childNodes.length > 0)
            {
               while(_loc3_ != null)
               {
                  if(_loc3_.nodeName == "loadingbanner")
                  {
                     _loc4_ = _loc3_.attributes.file;
                     xDoc.parent._aLoadingBannersFiles.push(_loc4_);
                  }
                  _loc3_ = _loc3_.nextSibling;
               }
            }
         }
         xDoc.parent._bLoadingBannersFilesLoaded = true;
         xDoc.parent.showBanner(xDoc.bShow);
      };
      xDoc.ignoreWhite = true;
      xDoc.bShow = bShow;
      xDoc.parent = this;
      xDoc.load(dofus.Constants.XML_LOADING_BANNERS_PATH);
   }
   function initLoader(mcRoot)
   {
      this._sPrefixURL = this._url.substr(0,this._url.lastIndexOf("/") + 1);
      _global.CONFIG = new dofus.utils.DofusConfiguration();
      this.clearlogs();
      this.showMainLogger(false);
      this.showShowLogsButton(false);
      this.showConfigurationChoice(false);
      this.showNextButton(false);
      this.showContinueButton(false);
      this.showClearCacheButton(false);
      this.showCopyLogsButton(false);
      this.showProgressBar(false);
      this._mcContainer = this.createEmptyMovieClip("__ANKDATA__",this.getNextHighestDepth());
      this._mcLocalFileList = this.createEmptyMovieClip("__ANKFILEDATA__",this.getNextHighestDepth());
      _global.CONFIG.isNewAccount = _root.htmlLogin != undefined && (_root.htmlPassword != undefined && (_root.htmlLogin != null && (_root.htmlPassword != null && (_root.htmlLogin != "null" && (_root.htmlPassword != "null" && (_root.htmlLogin != "" && _root.htmlPassword != ""))))));
      this._bNonCriticalError = false;
      this._bUpdate = false;
      this._sStep = null;
      ank.gapi.styles.StylesManager.loadStylePackage(ank.gapi.styles.DefaultStylePackage);
      ank.gapi.styles.StylesManager.loadStylePackage(dofus.graphics.gapi.styles.DofusStylePackage);
      ank.utils.Extensions.addExtensions();
      if(System.capabilities.playerType == "StandAlone")
      {
         Key.addListener(this);
      }
      this._mcModules = mcRoot.createEmptyMovieClip("mcModules",mcRoot.getNextHighestDepth());
      this._mclLoader = new MovieClipLoader();
      this._mclLoader.addListener(this);
      this.addToQueue({object:this,method:this.initTexts});
      this.addToQueue({object:this,method:this.initComponents});
      this.addToQueue({object:this,method:this.showBasicInformations,params:[true]});
   }
   function initComponents()
   {
      this._lblConnexionServer.text = this.getText("SERVER");
      this._lblConfiguration.text = this.getText("CONFIGURATION");
      this._mcTotalProgressBarGroup.txtInfo.text = "Loading";
      this._btnChoose.label = this.getText("VALID");
      this._btnChoose.addEventListener("click",this);
      this._btnContinue.label = this.getText("CONTINUE");
      this._btnContinue.addEventListener("click",this);
      this._btnClearCache.label = this.getText("CLEAR_CACHE");
      this._btnClearCache.addEventListener("click",this);
      this._btnNext.label = this.getText("NEXT");
      this._btnNext.addEventListener("click",this);
      this._btnShowLogs.label = this.getText("SHOW_LOGS");
      this._btnShowLogs.addEventListener("click",this);
      this._btnCopyLogsToClipbard.label = this.getText("COPY_LOGS");
      this._btnCopyLogsToClipbard.addEventListener("click",this);
      this._lstConfiguration.addEventListener("itemSelected",this);
      this._lstConnexionServer.addEventListener("itemSelected",this);
      this.launchBannerAnim(true);
   }
   function initTexts()
   {
      var _loc2_ = new dofus.DofusLoaderLogger();
      this.LANG_TEXT = _loc2_.langs;
      this.ERRORS = _loc2_.errors;
   }
   static function registerAllClasses()
   {
      Object.registerClass("ButtonNormalDown",ank.gapi.controls.button.ButtonBackground);
      Object.registerClass("ButtonNormalUp",ank.gapi.controls.button.ButtonBackground);
      Object.registerClass("ButtonToggleDown",ank.gapi.controls.button.ButtonBackground);
      Object.registerClass("ButtonToggleUp",ank.gapi.controls.button.ButtonBackground);
      Object.registerClass("ButtonSimpleRectangleUpDown",ank.gapi.controls.button.ButtonBackground);
      Object.registerClass("Label",ank.gapi.controls.Label);
      Object.registerClass("Button",ank.gapi.controls.Button);
      Object.registerClass("SelectableRow",ank.gapi.controls.list.SelectableRow);
      Object.registerClass("DefaultCellRenderer",ank.gapi.controls.list.DefaultCellRenderer);
      Object.registerClass("List",ank.gapi.controls.List);
      Object.registerClass("ConsoleLogger",ank.gapi.controls.ConsoleLogger);
      Object.registerClass("DofusLoader",dofus.DofusLoader);
      Object.registerClass("DofusLoader_TripleFramerate",dofus.DofusLoader);
      Object.registerClass("Loader",ank.gapi.controls.Loader);
   }
   function log(sText, sHColor, sLColor)
   {
      if(sHColor == undefined)
      {
         sHColor = "#CCCCCC";
      }
      if(sLColor == undefined)
      {
         sLColor = "#666666";
      }
      this._currentLogger.log(sText,sHColor,sLColor);
      this.addToSaveLog(sText);
   }
   function getDataBankLogHeader(nDataBank)
   {
      return "[DataBank " + nDataBank + "] ";
   }
   function addToSaveLog(sText)
   {
      this._sLogs += new ank.utils.ExtendedString(sText).replace("&nbsp;"," ") + "\r\n";
   }
   function logTitle(sText)
   {
      this.log("");
      this.log(sText,"#CCCCCC","#CCCCCC");
   }
   function logRed(sText)
   {
      this.log(sText,"#FF0000","#DD0000");
   }
   function logGreen(sText)
   {
      this.log(sText,"#00FF00","#00AA00");
   }
   function logOrange(sText)
   {
      this.log(sText,"#FF9900","#DD7700");
   }
   function logYellow(sText)
   {
      this.log(sText,"#FFFF00","#AAAA00");
   }
   function getText(key, aParams)
   {
      var _loc4_ = this.LANG_TEXT[key][_global.CONFIG.language];
      if(_loc4_ == undefined || _loc4_.length == 0)
      {
         _loc4_ = _global[dofus.Constants.GLOBAL_SO_LANG_NAME + "_" + dofus.utils.DofusTranslator.STANDARD_DATA_BANK].data[key];
      }
      if(_loc4_ == undefined || _loc4_.length == 0)
      {
         _loc4_ = this.LANG_TEXT[key].fr;
      }
      return this.replaceText(_loc4_,aParams);
   }
   function replaceText(sText, aParams)
   {
      if(aParams == undefined)
      {
         aParams = [];
      }
      var _loc4_ = [];
      var _loc5_ = [];
      var _loc6_ = 0;
      while(_loc6_ < aParams.length)
      {
         _loc4_.push("%" + (_loc6_ + 1));
         _loc5_.push(aParams[_loc6_]);
         _loc6_ = _loc6_ + 1;
      }
      return new ank.utils.ExtendedString(sText).replace(_loc4_,_loc5_);
   }
   function clearlogs()
   {
      this._cLogger.clear();
      this._cLoggerInit.clear();
      this._cLoggerError.clear();
   }
   function setProgressBarValue(nValue, nMax)
   {
      this.showProgressBar(true);
      if(nValue > nMax)
      {
         nValue = nMax;
      }
      this._mcProgressBarGroup.mcProgressBar._width = nValue / nMax * 100;
      this._mcProgressBarGroup.txtPercent.text = Math.floor(Number(this._mcProgressBarGroup.mcProgressBar._width)) + "%";
   }
   function showProgressBar(bShow)
   {
      if(this._mcProgressBarGroup._visible != bShow)
      {
         this._mcProgressBarGroup._visible = bShow;
      }
   }
   function moveProgressBar(nX)
   {
   }
   function showWaitBar(bShow)
   {
      if(bShow)
      {
         this._mcWaitBar = this.attachMovie("GrayWaitBar","_mcWaitBar",1000,{_x:this._mcProgressBarGroup._x + this._mcProgressBarGroup.mcProgressBarBorder._x,_y:this._mcProgressBarGroup._y + this._mcProgressBarGroup.mcProgressBarBorder._y});
         this._mcWaitBar.txtInfo.text = "Waiting";
      }
      else
      {
         this._mcWaitBar.removeMovieClip();
      }
      if(bShow)
      {
         this.showProgressBar(false);
      }
   }
   function setTotalBarValue(nValue, nMax)
   {
      this.showTotalBar(true);
      if(nValue > nMax)
      {
         nValue = nMax;
      }
      this._mcTotalProgressBarGroup.mcProgressBar._width = nValue / nMax * 100;
      this._mcTotalProgressBarGroup.txtPercent.text = Math.floor(Number(this._mcTotalProgressBarGroup.mcProgressBar._width)) + "%";
   }
   function showTotalBar(bShow)
   {
      var _loc3_;
      var _loc4_;
      var _loc5_;
      var _loc6_;
      var _loc7_;
      var _loc8_;
      if(bShow)
      {
         _loc3_ = 10079232;
         _loc4_ = (_loc3_ & 0xFF0000) >> 16;
         _loc5_ = (_loc3_ & 0xFF00) >> 8;
         _loc6_ = _loc3_ & 0xFF;
         _loc7_ = new Color(this._mcTotalProgressBarGroup.mcProgressBar);
         _loc8_ = {};
         _loc8_ = {ra:"0",rb:_loc4_,ga:"0",gb:_loc5_,ba:"0",bb:_loc6_,aa:"100",ab:"0"};
         _loc7_.setTransform(_loc8_);
         this._mcLoadingWindow._visible = true;
         this._mcTotalProgressBarGroup._visible = true;
      }
      else
      {
         this._mcTotalProgressBarGroup._visible = false;
         this._mcLoadingWindow._visible = false;
      }
   }
   function showConfigurationChoice(bShow)
   {
      this._lblConfiguration._visible = bShow;
      this._lstConfiguration._visible = bShow;
      this._lblConnexionServer._visible = bShow;
      this._lstConnexionServer._visible = bShow;
      this._btnChoose._visible = bShow;
   }
   function showNextButton(bShow)
   {
      this._btnNext._visible = bShow;
   }
   function showShowLogsButton(bShow)
   {
      this._btnShowLogs._visible = bShow;
   }
   function showContinueButton(bShow)
   {
      this._btnContinue._visible = bShow;
   }
   function showClearCacheButton(bShow)
   {
      this._btnClearCache._visible = bShow;
   }
   function showCopyLogsButton(bShow)
   {
      this._btnCopyLogsToClipbard._visible = bShow;
   }
   function showMainLogger(bShow)
   {
      if(bShow == undefined)
      {
         bShow = !this._cLogger._visible;
      }
      this._cLogger._visible = bShow;
   }
   function nonCriticalError(sError, sTab)
   {
      this.logOrange(sTab + "<b>" + this.getText("WARNING") + "</b> : " + sError);
      this._bNonCriticalError = true;
   }
   function criticalError(sError, sTab, bShowClearCacheButton, aParams, sFrom)
   {
      var _loc7_ = this.ERRORS[sError];
      this.ERRORS.current = sError;
      this.ERRORS.from = sFrom;
      var _loc8_ = this.replaceText(_loc7_[_global.CONFIG.language],aParams);
      if(_loc8_ == undefined || _loc8_.length == 0)
      {
         _loc8_ = this.replaceText(_loc7_.fr,aParams);
      }
      this._cLoggerError.log("<b>" + this.getText("ERROR") + "</b> : " + _loc8_,"#FF0000","#DD0000");
      var _loc9_ = "<u><a href=\'" + _loc7_["link" + _global.CONFIG.language] + "\' target=\'_blank\'>" + this.getText("LINK_HELP") + "</a></u>";
      this._cLoggerError.log(_loc9_,"#FF0000","#DD0000");
      this.addToSaveLog(sTab + "<b>" + this.getText("ERROR") + "</b> : " + _loc8_);
      this.showCopyLogsButton(true);
      this.showShowLogsButton(true);
      this.showContinueButton(true);
      if(bShowClearCacheButton)
      {
         this.showClearCacheButton(true);
      }
   }
   function getLangSharedObject(nDataBank)
   {
      return ank.utils.SharedObjectFix.getLocal(dofus.Constants.LANG_SHAREDOBJECT_NAME + "_" + nDataBank);
   }
   function getXtraSharedObject(nDataBank)
   {
      return ank.utils.SharedObjectFix.getLocal(dofus.Constants.XTRA_SHAREDOBJECT_NAME + "_" + nDataBank);
   }
   function getOptionsSharedObject()
   {
      return ank.utils.SharedObjectFix.getLocal(dofus.Constants.GLOBAL_SO_OPTIONS_NAME);
   }
   function getShortcutsSharedObject()
   {
      return ank.utils.SharedObjectFix.getLocal(dofus.Constants.GLOBAL_SO_SHORTCUTS_NAME);
   }
   function getOccurencesSharedObject()
   {
      return ank.utils.SharedObjectFix.getLocal(dofus.Constants.GLOBAL_SO_OCCURENCES_NAME);
   }
   function getCacheDateSharedObject()
   {
      return ank.utils.SharedObjectFix.getLocal(dofus.Constants.GLOBAL_SO_CACHEDATE_NAME);
   }
   function launchBannerAnim(bPlay)
   {
      if(!this._bBannerDisplay)
      {
         this.showBanner(true);
      }
      if(bPlay)
      {
         this._mcBanner.playAll();
      }
      else
      {
         this._mcBanner.stopAll();
      }
   }
   function showBanner(bShow)
   {
      var _loc3_;
      var _loc4_;
      var _loc6_;
      var _loc7_;
      var _loc5_;
      var _loc8_;
      if(!this._bLoadingBannersFilesLoaded)
      {
         this.addLoadingBannersFiles(bShow);
      }
      else
      {
         _loc3_ = bShow != undefined ? bShow : !this._bBannerDisplay;
         if(_loc3_)
         {
            if(this._bBannerDisplay)
            {
               return undefined;
            }
            _loc4_ = "";
            if(this._aLoadingBannersFiles.length > 0)
            {
               _loc6_ = Math.floor(Math.random() * (this._aLoadingBannersFiles.length + 1));
               if(_loc6_ < this._aLoadingBannersFiles.length)
               {
                  _loc7_ = this._aLoadingBannersFiles[_loc6_];
                  _loc5_ = this.createEmptyMovieClip("_mcBanner",this.getNextHighestDepth());
                  org.utils.Bitmap.loadBitmapSmoothed(dofus.Constants.LOADING_BANNERS_PATH + _loc7_,_loc5_);
               }
            }
            _loc8_ = "";
            if(!_loc5_)
            {
               _loc5_ = this.attachMovie("LoadingBanner_" + _global.CONFIG.language,"_mcBanner",this.getNextHighestDepth(),this._mcBannerPlacer);
            }
            if(!_loc5_)
            {
               _loc5_ = this.attachMovie("LoadingBanner_" + _loc8_,"_mcBanner",this.getNextHighestDepth(),this._mcBannerPlacer);
            }
            if(!_loc5_)
            {
               _loc5_ = this.attachMovie("LoadingBanner","_mcBanner",this.getNextHighestDepth(),this._mcBannerPlacer);
            }
            _loc5_.cacheAsBitmap = true;
            _loc5_.swapDepths(this._mcBannerPlacer);
         }
         else
         {
            if(!this._bBannerDisplay)
            {
               return undefined;
            }
            this._mcBanner.swapDepths(this._mcBannerPlacer);
            this._mcBanner.removeMovieClip();
         }
         this._bBannerDisplay = _loc3_;
      }
   }
   function copyAndOrganizeDataServersForDataBank(nDataBank)
   {
      var _loc3_ = _global.CONFIG.dataBanks;
      var _loc4_ = _loc3_[nDataBank].slice(0);
      var _loc5_ = 0;
      var _loc6_;
      var _loc7_;
      while(_loc5_ < _loc4_.length)
      {
         _loc6_ = _loc4_[_loc5_];
         if(_loc6_.nPriority == undefined || _global.isNaN(_loc6_.nPriority))
         {
            _loc6_.nPriority = 0;
         }
         _loc7_ = _loc6_.priority;
         _loc6_.rand = random(99999);
         _loc5_ = _loc5_ + 1;
      }
      _loc4_.sortOn(["priority","rand"],Array.DESCENDING);
      var _loc8_ = 0;
      while(_loc8_ < _loc4_.length)
      {
         _loc8_ = _loc8_ + 1;
      }
      return _loc4_;
   }
   function copyAndOrganizeDataBanks()
   {
      var _loc2_ = [];
      var _loc3_ = _global.CONFIG.dataBanks;
      var _loc4_ = 0;
      while(_loc4_ < _loc3_.length)
      {
         _loc2_[_loc4_] = _loc3_[_loc4_].slice(0);
         _loc4_ = _loc4_ + 1;
      }
      var _loc5_ = 0;
      var _loc6_;
      var _loc7_;
      var _loc8_;
      var _loc9_;
      var _loc10_;
      while(_loc5_ < _loc2_.length)
      {
         _loc6_ = _loc2_[_loc5_];
         _loc7_ = 0;
         while(_loc7_ < _loc6_.length)
         {
            _loc8_ = _loc6_[_loc7_];
            if(_loc8_.nPriority == undefined || _global.isNaN(_loc8_.nPriority))
            {
               _loc8_.nPriority = 0;
            }
            _loc9_ = _loc8_.priority;
            _loc8_.rand = random(99999);
            _loc7_ = _loc7_ + 1;
         }
         _loc6_.sortOn(["priority","rand"],Array.DESCENDING);
         _loc10_ = 0;
         while(_loc10_ < _loc6_.length)
         {
            _loc10_ = _loc10_ + 1;
         }
         _loc5_ = _loc5_ + 1;
      }
      return _loc2_;
   }
   function checkOccurences()
   {
      var _loc2_ = _global.API.lang.getConfigText("MAXIMUM_CLIENT_OCCURENCES");
      if(_loc2_ == undefined || (_global.isNaN(_loc2_) || _loc2_ < 1))
      {
         return true;
      }
      var _loc3_ = this.getOccurencesSharedObject().data.occ;
      var _loc4_ = [];
      var _loc5_ = 0;
      while(_loc5_ < _loc3_.length)
      {
         if(_loc3_[_loc5_].tick + dofus.Constants.MAX_OCCURENCE_DELAY > new Date().getTime())
         {
            _loc4_.push(_loc3_[_loc5_]);
         }
         _loc5_ = _loc5_ + 1;
      }
      var _loc6_ = _loc4_.length;
      if(!_global.API.datacenter.Player.isAuthorized && _loc6_ + 1 > _loc2_)
      {
         this.criticalError("TOO_MANY_OCCURENCES",this.TABULATION,false);
         return false;
      }
      this._nOccurenceId = Math.round(Math.random() * 1000);
      _loc4_.push({id:this._nOccurenceId,tick:new Date().getTime()});
      this.getOccurencesSharedObject().data.occ = _loc4_;
      _global.setInterval(this,"refreshOccurenceTick",dofus.Constants.OCCURENCE_REFRESH);
      return true;
   }
   function refreshOccurenceTick()
   {
      var _loc2_ = this.getOccurencesSharedObject().data.occ;
      var _loc3_ = 0;
      while(_loc3_ < _loc2_.length)
      {
         if(_loc2_[_loc3_].id == this._nOccurenceId)
         {
            _loc2_[_loc3_].tick = new Date().getTime();
            break;
         }
         _loc3_ = _loc3_ + 1;
      }
      this.getOccurencesSharedObject().data.occ = _loc2_;
   }
   function checkFlashPlayer()
   {
      var _loc2_ = System.capabilities.version;
      var _loc3_ = Number(_loc2_.split(" ")[1].split(",")[0]);
      var _loc5_;
      var _loc6_;
      var _loc4_;
      if(_root.electron != undefined)
      {
         _loc5_ = String(flash.external.ExternalInterface.call("getElectronVersion"));
         _loc6_ = String(flash.external.ExternalInterface.call("getNodejsVersion"));
         _loc4_ = " (Electron <b>" + _loc5_ + "</b> | Node.js <b>" + _loc6_ + "</b>)";
      }
      else
      {
         _loc4_ = System.capabilities.playerType.length != 0 ? " (" + System.capabilities.playerType + ")" : " ";
      }
      var _loc7_ = "Flash player" + _loc4_ + " <b>" + _loc2_ + "</b>";
      this.log(this.TABULATION + _loc7_);
      if(dofus.Constants.USE_JS_LOG && _global.CONFIG.isNewAccount)
      {
         this.getURL("JavaScript:WriteLog(\'checkFlashPlayer;" + _loc3_ + "\')");
         this.getURL("JavaScript:WriteLog(\'versionDate;" + dofus.Constants.VERSIONDATE + "\')");
      }
      var _loc8_;
      if(_loc3_ >= 8)
      {
         _loc8_ = System.security.sandboxType;
         if(_loc8_ != "localTrusted" && _loc8_ != "remote")
         {
            this.criticalError("BAD_FLASH_SANDBOX",this.TABULATION,false);
            return false;
         }
         return true;
      }
      this.criticalError("BAD_FLASH_PLAYER",this.TABULATION,false);
      this.showBanner(false);
      return false;
   }
   function click(oEvent)
   {
      var _loc3_;
      var _loc4_;
      switch(oEvent.target)
      {
         case this._btnChoose:
            this.chooseConfiguration(this._lstConfiguration.selectedItem.data,this._lstConnexionServer.selectedItem.data,true);
            break;
         case this._btnClearCache:
            this.clearCache();
            this.reboot();
            break;
         case this._btnCopyLogsToClipbard:
            System.setClipboard(this._sLogs);
            break;
         case this._btnShowLogs:
            this.showBanner(false);
            this.showMainLogger();
            break;
         case this._btnContinue:
            switch(this.ERRORS.current)
            {
               case "CHECK_LAST_VERSION_FAILED":
                  _loc3_ = new LoadVars();
                  _loc3_.f = "";
                  this.onCheckLanguage(true,_loc3_,"","");
                  break;
               case "CHECK_LAST_VERSION_FAILED":
                  _loc4_ = new LoadVars();
                  _loc4_.f = "";
                  this.onCheckLanguage(true,_loc4_,"","");
            }
            break;
         case this._btnNext:
            this.showNextButton(false);
            switch(this._sStep)
            {
               case "MODULE":
                  this.initCore(_global.MODULE_CORE);
                  break;
               case "XTRA":
                  this.initAndLoginFinished();
            }
         default:
            return;
      }
   }
   function itemSelected(oEvent)
   {
      switch(oEvent.target)
      {
         case this._lstConfiguration:
            this.selectConfiguration();
            break;
         case this._lstConnexionServer:
            this.selectConnexionServer();
         default:
            return;
      }
   }
   function onKeyUp()
   {
      if(Key.getCode() == Key.ESCAPE)
      {
         fscommand("quit","");
      }
   }
   function setDisplayStyle(sStyleName)
   {
      if(System.capabilities.playerType == "PlugIn" && (!_global.CONFIG.isStreaming && _root.electron == undefined))
      {
         this.getURL("javascript:setFlashStyle(\'flashid\', \'" + sStyleName + "\');");
      }
   }
   function closeBrowserWindow()
   {
      if(System.capabilities.playerType == "PlugIn")
      {
         this.getURL("javascript:closeBrowserWindow();");
      }
   }
   function reboot()
   {
      var _loc2_ = 0;
      while(_loc2_ < dofus.Constants.MODULES_LIST.length)
      {
         this._mclLoader.unloadClip(_global["MODULE_" + dofus.Constants.MODULES_LIST[_loc2_][4]]);
         _loc2_ = _loc2_ + 1;
      }
      dofus.DofusCore.getClip().removeMovieClip();
      this.initLoader(_root);
   }
   function clearCache()
   {
      var _loc2_ = this.getOptionsSharedObject();
      var _loc3_ = _loc2_.data.dataBanksCount;
      if(_loc3_ == undefined || _global.isNaN(_loc3_))
      {
         return undefined;
      }
      var _loc4_ = 0;
      var _loc5_;
      var _loc6_;
      while(_loc4_ < _loc3_)
      {
         _loc5_ = this.getLangSharedObject(_loc4_);
         _loc6_ = this.getXtraSharedObject(_loc4_);
         _loc5_.clear();
         _loc6_.clear();
         _loc4_ = _loc4_ + 1;
      }
   }
   function showLoader(bShow, bNotClear)
   {
      this._visible = bShow;
   }
   function showBasicInformations(bContinue)
   {
      this._currentLogger = this._cLoggerInit;
      this.logTitle(this.getText("STARTING"));
      this.log(this.TABULATION + "Dofus Retro <b>v" + dofus.Constants.VERSION + "." + dofus.Constants.SUBVERSION + "." + dofus.Constants.SUBSUBVERSION + "</b> " + (dofus.Constants.BETAVERSION <= 0 ? "" : "(<font color=\"#FF0000\"><i><b>BETA " + dofus.Constants.BETAVERSION + "</b></i></font>) ") + "(<b>" + dofus.Constants.VERSIONDATE + "</b>" + (!dofus.Constants.ALPHA ? "" : " <font color=\"#00FF00\"><i><b>ALPHA BUILD</b></i></font>") + ")");
      if(!this.checkFlashPlayer())
      {
         this.showShowLogsButton(false);
         this.showCopyLogsButton(false);
         return undefined;
      }
      this.checkCacheVersion();
      this._currentLogger = this._cLogger;
      if(bContinue)
      {
         this.addToQueue({object:this,method:this.loadConfig});
      }
   }
   function loadConfig()
   {
      this.showLoader(true);
      this.moveProgressBar(0);
      this.logTitle(this.getText("LOADING_CONFIG_FILE"));
      var _loc2_ = new XML();
      var loader = this;
      _loc2_.ignoreWhite = true;
      _loc2_.onLoad = function(bSuccess)
      {
         loader.onConfigLoaded(bSuccess,this);
      };
      this.showWaitBar(true);
      if(!dofus.Electron.getUserDataTextFileXMLContent(_loc2_,dofus.Constants.CONFIG_XML_FILE))
      {
         _loc2_.load(dofus.Constants.CONFIG_XML_FILE);
      }
   }
   function onConfigLoaded(bSuccess, xDoc)
   {
      this.showWaitBar(false);
      if(dofus.Constants.USE_JS_LOG && _global.CONFIG.isNewAccount)
      {
         this.getURL("JavaScript:WriteLog(\'onConfigLoaded;" + bSuccess + "\')");
      }
      if(!bSuccess)
      {
         this.criticalError("NO_CONFIG_FILE",this.TABULATION,false);
         return undefined;
      }
      this.setTotalBarValue(50,100);
      var _loc4_ = xDoc.firstChild.firstChild;
      if(xDoc.childNodes.length == 0 || _loc4_ == null)
      {
         this.criticalError("CORRUPT_CONFIG_FILE",this.TABULATION,false);
         return undefined;
      }
      _global.CONFIG.cacheAsBitmap = [];
      var _loc5_ = new ank.utils.ExtendedArray();
      var _loc6_ = false;
      var _loc7_;
      var _loc8_;
      var _loc9_;
      var _loc10_;
      var _loc11_;
      var _loc12_;
      var _loc13_;
      var _loc14_;
      var _loc15_;
      var _loc0_;
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
      while(_loc4_ != null)
      {
         switch(_loc4_.nodeName)
         {
            case "delay":
               _global.CONFIG.delay = _loc4_.attributes.value;
               break;
            case "rdelay":
               _global.CONFIG.rdelay = _loc4_.attributes.value;
               break;
            case "rcount":
               _global.CONFIG.rcount = _loc4_.attributes.value;
               break;
            case "hardcore":
               _global.CONFIG.onlyHardcore = true;
               break;
            case "streaming":
               _global.CONFIG.isStreaming = true;
               if(_loc4_.attributes.method)
               {
                  _global.CONFIG.streamingMethod = _loc4_.attributes.method;
               }
               else
               {
                  _global.CONFIG.streamingMethod = "compact";
               }
               _root._misc.attachMovie("UI_Misc","miniClip",_root._misc.getNextHighestDepth());
               break;
            case "expo":
               _global.CONFIG.isExpo = true;
               break;
            case "conf":
               _loc7_ = _loc4_.attributes.name;
               _loc8_ = _loc4_.attributes.type;
               if(_loc7_ != undefined && (dofus.Constants.TEST != true && _loc8_ != "test" || dofus.Constants.TEST == true && _loc8_ == "test"))
               {
                  _loc9_ = {};
                  _loc9_.name = _loc7_;
                  _loc10_ = Number(_loc4_.attributes.zaapconnectport);
                  _loc9_.zaapConnectPort = !(_loc10_ == undefined || _global.isNaN(_loc10_)) ? _loc10_ : dofus.ZaapConnect.TCP_DEFAULT_PORT;
                  _loc9_.debug = _loc4_.attributes.boo == "1";
                  _loc9_.debugRequests = _loc4_.attributes.debugrequests == "1" || _loc4_.attributes.debugrequests == "2";
                  _loc9_.logRequests = _loc4_.attributes.debugrequests == "2";
                  _loc9_.openRetroChat = _loc4_.attributes.openRetroChat == "1";
                  _loc9_.openRetroConsole = _loc4_.attributes.openRetroConsole == "1";
                  _loc9_.connexionServers = new ank.utils.ExtendedArray();
                  _loc9_.dataBanks = [];
                  _loc11_ = _loc9_.dataBanks;
                  _loc12_ = _loc4_.firstChild;
                  while(_loc12_ != null)
                  {
                     switch(_loc12_.nodeName)
                     {
                        case "databank":
                           _loc13_ = Number(_loc12_.attributes.id);
                           if(_global.isNaN(_loc13_))
                           {
                              break;
                           }
                           _loc14_ = _loc11_[_loc13_];
                           if(_loc14_ == undefined)
                           {
                              _loc14_ = [];
                              _loc11_[_loc13_] = _loc14_;
                           }
                           _loc15_ = _loc12_.firstChild;
                           while(_loc15_ != null)
                           {
                              if((_loc0_ = _loc15_.nodeName) === "dataserver")
                              {
                                 _loc16_ = _loc15_.attributes.url;
                                 _loc17_ = _loc15_.attributes.type;
                                 _loc18_ = Number(_loc15_.attributes.priority);
                                 if(_loc16_ != undefined && _loc16_ != "")
                                 {
                                    _loc14_.push({url:_loc16_,type:_loc17_,priority:_loc18_,dataBankId:_loc13_});
                                    System.security.allowDomain(_loc16_);
                                 }
                              }
                              _loc15_ = _loc15_.nextSibling;
                           }
                           _loc19_ = this.getOptionsSharedObject();
                           _loc19_.data.dataBanksCount = _loc14_.length;
                           _loc19_.flush();
                           break;
                        case "dataserver":
                           _loc20_ = dofus.utils.DofusTranslator.STANDARD_DATA_BANK;
                           _loc21_ = _loc11_[_loc20_];
                           if(_loc21_ == undefined)
                           {
                              _loc21_ = [];
                              _loc11_[_loc20_] = _loc21_;
                           }
                           _loc22_ = _loc12_.attributes.url;
                           _loc23_ = _loc12_.attributes.type;
                           _loc24_ = Number(_loc12_.attributes.priority);
                           if(_loc22_ != undefined && _loc22_ != "")
                           {
                              _loc21_.push({url:_loc22_,type:_loc23_,priority:_loc24_,dataBankId:_loc20_});
                              System.security.allowDomain(_loc22_);
                           }
                           _loc25_ = this.getOptionsSharedObject();
                           _loc25_.data.dataBanksCount = _loc21_.length;
                           _loc25_.flush();
                           break;
                        case "connserver":
                           _loc26_ = _loc12_.attributes.name;
                           _loc27_ = _loc12_.attributes.ip;
                           _loc28_ = _loc12_.attributes.port;
                           if(_loc26_ != undefined && (_loc27_ != "" && _loc28_ != undefined))
                           {
                              _loc9_.connexionServers.push({label:_loc26_,data:{name:_loc26_,ip:_loc27_,port:_loc28_}});
                           }
                           break;
                        default:
                           this.nonCriticalError(this.getText("UNKNOWN_TYPE_NODE") + " (" + _loc4_.nodeName + ")",this.TABULATION);
                     }
                     _loc12_ = _loc12_.nextSibling;
                  }
                  if(_loc11_[dofus.utils.DofusTranslator.STANDARD_DATA_BANK].length > 0)
                  {
                     _loc5_.push({label:_loc9_.name,data:_loc9_});
                  }
               }
               break;
            case "languages":
               _global.CONFIG.xmlLanguages = _loc4_.attributes.value.split(",");
               _global.CONFIG.skipLanguageVerification = _loc4_.attributes.skipcheck == "true" || _loc4_.attributes.skipcheck == "1";
               break;
            case "cacheasbitmap":
               _loc29_ = _loc4_.firstChild;
               while(_loc29_ != null)
               {
                  _loc30_ = _loc29_.attributes.element;
                  _loc31_ = _loc29_.attributes.value == "true";
                  _global.CONFIG.cacheAsBitmap[_loc30_] = _loc31_;
                  _loc29_ = _loc29_.nextSibling;
               }
               break;
            case "servers":
               _loc32_ = _loc4_.firstChild;
               _global.CONFIG.customServersIP = [];
               while(_loc32_ != null)
               {
                  _loc33_ = _loc32_.attributes.id;
                  _loc34_ = _loc32_.attributes.ip;
                  _loc35_ = _loc32_.attributes.port;
                  _global.CONFIG.customServersIP[_loc33_] = {ip:_loc34_,port:_loc35_};
                  _loc32_ = _loc32_.nextSibling;
               }
               break;
            default:
               this.nonCriticalError(this.getText("UNKNOWN_TYPE_NODE") + " (" + _loc4_.nodeName + ")",this.TABULATION);
         }
         _loc4_ = _loc4_.nextSibling;
      }
      if(_loc5_.length == 0)
      {
         this.criticalError("CORRUPT_CONFIG_FILE",this.TABULATION,false);
         return undefined;
      }
      this.log(this.TABULATION + this.getText("CONFIG_FILE_LOADED"));
      this.askForConfiguration(_loc5_);
   }
   function askForConfiguration(eaConfigurations)
   {
      var _loc3_;
      var _loc4_;
      if(eaConfigurations.length == 1 && eaConfigurations[0].data.connexionServers.length == 0)
      {
         this.chooseConfiguration(eaConfigurations[0].data,undefined,false);
      }
      else
      {
         this.logTitle(this.getText("CHOOSE_CONFIGURATION"));
         this._lstConfiguration.dataProvider = eaConfigurations;
         _loc3_ = this.getOptionsSharedObject().data.loaderLastConfName;
         if(_loc3_ != undefined)
         {
            _loc4_ = 0;
            while(_loc4_ < eaConfigurations.length)
            {
               if(eaConfigurations[_loc4_].data.name == _loc3_)
               {
                  this._lstConfiguration.selectedIndex = _loc4_;
                  break;
               }
               _loc4_ = _loc4_ + 1;
            }
         }
         else
         {
            this._lstConfiguration.selectedIndex = 0;
         }
         this.selectConfiguration();
         this.showConfigurationChoice(true);
      }
   }
   function selectConfiguration()
   {
      var _loc2_ = this._lstConfiguration.selectedItem.data.connexionServers;
      this._lstConnexionServer.dataProvider = _loc2_;
      var _loc3_ = this.getOptionsSharedObject();
      var _loc4_ = _loc3_.data.loaderConf[this._lstConfiguration.selectedItem.label];
      var _loc5_;
      if(_loc4_ != undefined)
      {
         _loc5_ = 0;
         while(_loc5_ < _loc2_.length)
         {
            if(_loc2_[_loc5_].data.name == _loc4_)
            {
               this._lstConnexionServer.selectedIndex = _loc5_;
               break;
            }
            _loc5_ = _loc5_ + 1;
         }
      }
      else if(_loc2_.length > 0)
      {
         this._lstConnexionServer.selectedIndex = 0;
      }
      var _loc6_ = this._lstConfiguration.selectedItem.label;
      var _loc7_ = _loc6_ != _loc3_.data.loaderLastConfName;
      if(_loc7_)
      {
         this.clearCache();
      }
      _loc3_.data.loaderLastConfName = _loc6_;
      _loc3_.flush();
      this.selectConnexionServer();
   }
   function selectConnexionServer()
   {
      var _loc2_ = this.getOptionsSharedObject();
      if(_loc2_.data.loaderConf == undefined)
      {
         _loc2_.data.loaderConf = {};
      }
      _loc2_.data.loaderConf[this._lstConfiguration.selectedItem.label] = this._lstConnexionServer.selectedItem.label;
      _loc2_.flush();
   }
   function chooseConfiguration(oConf, oServer, bLog)
   {
      this.showConfigurationChoice(false);
      if(bLog)
      {
         this.log(this.TABULATION + this.getText("CURRENT_CONFIG",[oConf.name]));
         if(oServer != undefined)
         {
            this.log(this.TABULATION + this.getText("CURRENT_SERVER",[oServer.name]));
         }
      }
      _global.CONFIG.dataBanks = oConf.dataBanks;
      _global.CONFIG.connexionServer = oServer;
      _global.CONFIG.zaapConnectPort = oConf.zaapConnectPort;
      if(oConf.debug)
      {
         dofus.Constants.DEBUG = true;
         this.logYellow(this.TABULATION + this.getText("DEBUG_MODE"));
      }
      if(oConf.debugRequests)
      {
         dofus.Constants.DEBUG_DATAS = true;
      }
      if(oConf.logRequests)
      {
         dofus.Constants.LOG_DATAS = true;
      }
      if(oConf.openRetroChat)
      {
         dofus.Electron.retroChatOpen();
      }
      if(oConf.openRetroConsole)
      {
         dofus.Electron.retroConsoleOpen();
      }
      dofus.ZaapConnect.newInstance();
      this.loadLocalFileList();
   }
   function startJsTimer()
   {
      this._nTimerJs = this._nTimerJs - 1;
      if(this._nTimerJs <= 0)
      {
         this._nTimerJs = 20;
         this.getURL("javascript:startTimer()");
      }
      if(this._bJsTimer)
      {
         this.addToQueue({object:this,method:this.startJsTimer});
      }
   }
   function loadLanguage()
   {
      if(dofus.Constants.USE_JS_LOG && _global.CONFIG.isNewAccount)
      {
         this.getURL("javascript:startTimer()");
         this.startJsTimer();
      }
      this.logTitle(this.getText("LOAD_LANG_FILE"));
      this._sStep = "LANG";
      this._nLoadedLangFiles = 0;
      var _loc2_ = this.copyAndOrganizeDataBanks();
      this._aCurrentDataBanks = _loc2_;
      var _loc3_ = 0;
      var _loc4_;
      var _loc5_;
      var _loc6_;
      var _loc7_;
      while(_loc3_ < _loc2_.length)
      {
         _loc4_ = this.getLangSharedObject(_loc3_);
         _loc5_ = _loc4_.data.VERSIONS.lang;
         _global[dofus.Constants.GLOBAL_SO_LANG_NAME + "_" + _loc3_] = _loc4_;
         _loc6_ = this.getDataBankLogHeader(_loc3_);
         this.log(this.TABULATION + _loc6_ + this.getText("CURRENT_LANG_FILE_VERSION",[_loc5_ != undefined ? _loc5_ : "Aucune"]));
         this.log(this.TABULATION + _loc6_ + this.getText("CHECK_LAST_VERSION"));
         _loc7_ = this._aXtraCurrentVersion[_loc3_];
         if(_loc7_ == undefined)
         {
            _loc7_ = [];
            this._aXtraCurrentVersion[_loc3_] = _loc7_;
         }
         _loc7_.lang = !_global.isNaN(_loc5_) ? Number(_loc5_) : 0;
         this.checkLanguageWithNextHost("lang," + _loc5_,_loc3_);
         _loc3_ = _loc3_ + 1;
      }
   }
   function checkLanguageWithNextHost(sFiles, nDataBank)
   {
      var _loc2_ = this._aCurrentDataBanks[nDataBank];
      var _loc3_;
      var _loc4_;
      var _loc5_;
      if(_loc2_.length < 1)
      {
         if(!this._bLocalFileListLoaded)
         {
            this.criticalError("CHECK_LAST_VERSION_FAILED",this.TABULATION,true,[],"checkXtra");
         }
         else
         {
            this.nonCriticalError("CHECK_LAST_VERSION_FAILED",this.TABULATION,true);
            _loc3_ = new LoadVars();
            _loc4_ = [];
            _loc5_ = this._mcLocalFileList.VERSIONS[_global.CONFIG.language];
            for(var i in _loc5_)
            {
               _loc4_.push(i + "," + _global.CONFIG.language + "," + _loc5_[i]);
            }
            _loc3_.f = _loc4_.join("|");
            this.onCheckLanguage(true,_loc3_,undefined,undefined,nDataBank);
         }
         return undefined;
      }
      var oServer = _loc2_.shift();
      if(oServer.type == "local")
      {
         this.checkLanguageWithNextHost(sFiles,nDataBank);
         return undefined;
      }
      var sURL = oServer.url + "lang/versions_" + _global.CONFIG.language + ".txt" + "?wtf=" + Math.random();
      var _loc6_ = new LoadVars();
      var loader = this;
      _loc6_.onLoad = function(bSuccess)
      {
         if(!bSuccess)
         {
            loader.nonCriticalError(loader.getText("IMPOSSIBLE_TO_GET_FILE",[sURL]),loader.TABULATION + loader.TABULATION);
         }
         loader.onCheckLanguage(bSuccess,this,oServer.url,sFiles,nDataBank);
      };
      this.showWaitBar(true);
      _loc6_.load(sURL,this,"GET");
   }
   function onCheckLanguage(bSuccess, lv, sServer, sFiles, nDataBank)
   {
      this.showWaitBar(false);
      var _loc7_;
      var _loc8_;
      var _loc9_;
      if(bSuccess && lv.f != undefined)
      {
         this.setTotalBarValue(100,100);
         this._aDistantFilesList[nDataBank] = lv.f;
         _loc7_ = lv.f.substr(lv.f.indexOf("lang,")).split("|")[0].split(",");
         _loc8_ = false;
         if(lv.f != "")
         {
            _loc9_ = _loc7_[2];
            if(_global.CONFIG.language == this.getLangSharedObject(nDataBank).data.LANGUAGE && (this._aXtraCurrentVersion[nDataBank].lang != undefined && _loc9_ == this._aXtraCurrentVersion[nDataBank].lang))
            {
               _loc8_ = true;
            }
            else
            {
               this.log(this.TABULATION + this.getDataBankLogHeader(nDataBank) + this.getText("NEW_LANG_FILE_AVAILABLE",[_loc7_[2]]));
               if(this._bSkipDistantLoad)
               {
                  if(this._aXtraCurrentVersion[nDataBank].lang == 0)
                  {
                     _loc9_ = this._mcLocalFileList.VERSIONS[_global.CONFIG.language].lang;
                  }
               }
               this.updateLanguage(_loc7_[2],nDataBank);
            }
         }
         else
         {
            _loc8_ = true;
         }
         if(_loc8_)
         {
            this._nLoadedLangFiles = this._nLoadedLangFiles + 1;
            this.log(this.TABULATION + this.getText("NO_NEW_VERSION_AVAILABLE"));
            if(this._aCurrentDataBanks.length == this._nLoadedLangFiles)
            {
               this.loadModules();
            }
         }
      }
      else
      {
         this.nonCriticalError(this.getText("IMPOSSIBLE_TO_JOIN_SERVER",[sServer]),this.TABULATION + this.TABULATION);
         this.checkLanguageWithNextHost(sFiles,nDataBank);
      }
   }
   function updateLanguage(nFileNumber, nDataBank)
   {
      this._bUpdate = true;
      this.showWaitBar(true);
      var _loc4_ = new dofus.utils.LangFileLoader();
      _loc4_.addListener(this);
      var _loc5_ = dofus.Constants.LANG_SHAREDOBJECT_NAME + "_" + nDataBank;
      var _loc6_ = this.copyAndOrganizeDataServersForDataBank(nDataBank);
      var _loc7_ = this.getDataBankMcContainer(nDataBank);
      _loc4_.loadLangFile(_loc6_,"lang/swf/lang_" + _global.CONFIG.language + "_" + nFileNumber + ".swf",_loc7_,_loc5_,"lang",_global.CONFIG.language,false);
   }
   function getDataBankMcContainer(nDataBank)
   {
      var _loc3_ = "db" + nDataBank;
      var _loc4_ = this._mcContainer[_loc3_];
      if(_loc4_ == undefined)
      {
         _loc4_ = this._mcContainer.createEmptyMovieClip(_loc3_,this._mcContainer.getNextHighestDepth());
      }
      return _loc4_;
   }
   function loadModules()
   {
      this.logTitle(this.getText("LOAD_MODULES"));
      this._sStep = "MODULE";
      this._aCurrentModules = dofus.Constants.MODULES_LIST.slice(0);
      this.loadNextModule();
   }
   function loadNextModule()
   {
      if(this._aCurrentModules.length < 1)
      {
         this.logTitle(this.getText("INIT_END"));
         this.onCoreLoaded(_global.MODULE_CORE);
         return undefined;
      }
      this._aCurrentModule = this._aCurrentModules.shift();
      var _loc2_ = this._aCurrentModule[0];
      var _loc3_ = this._aCurrentModule[1];
      var _loc4_ = this._aCurrentModule[2];
      var _loc5_ = this._aCurrentModule[4];
      this._mcCurrentModule = this._mcModules.createEmptyMovieClip("mc" + _loc5_,this._mcModules.getNextHighestDepth());
      this._timedProgress = _global.setInterval(this.onTimedProgress,1000,this,this._mclLoader,this._mcCurrentModule);
      this._mclLoader.loadClip(_loc3_,this._mcCurrentModule);
   }
   function onCoreLoaded(mcCore)
   {
      if(_global.CONFIG.isStreaming)
      {
         this._bJsTimer = false;
         this.getURL("javascript:stopTimer()");
      }
      if((this._bNonCriticalError || this._bUpdate) && (dofus.Constants.DEBUG && dofus.Kernel.FAST_SWITCHING_SERVER_REQUEST == undefined))
      {
         this.showNextButton(true);
         this.showCopyLogsButton(true);
         this.showShowLogsButton(true);
      }
      else
      {
         this.initCore(mcCore);
      }
   }
   function initCore(mcCore)
   {
      Key.removeListener(this);
      var _loc3_;
      if((_loc3_ = dofus.DofusCore.getInstance()) == undefined)
      {
         _loc3_ = new dofus.DofusCore(mcCore);
         if(Key.isDown(Key.SHIFT))
         {
            Stage.scaleMode = "exactFit";
         }
      }
      _loc3_.initStart();
      this._bNonCriticalError = false;
      this._bUpdate = false;
   }
   function loadLocalFileList()
   {
      this.logTitle(this.getText("LOAD_XTRA_FILES"));
      this._aCurrentDataBanks = this.copyAndOrganizeDataBanks();
      this.checkLocalFileListWithNextHost(dofus.Constants.LANG_LOCAL_FILE_LIST);
      this.showWaitBar(true);
   }
   function checkLocalFileListWithNextHost(sFiles)
   {
      var _loc2_ = this._aCurrentDataBanks[dofus.utils.DofusTranslator.STANDARD_DATA_BANK];
      if(_loc2_.length < 1)
      {
         this.nonCriticalError("CHECK_LAST_VERSION_FAILED",this.TABULATION + this.TABULATION,true);
         this.loadLanguage();
         return undefined;
      }
      var _loc3_ = _loc2_.shift();
      var sURL = _loc3_.url + sFiles;
      var loader = this;
      var _loc4_ = new MovieClipLoader();
      var _loc5_ = {};
      _loc5_.onLoadInit = function(mc)
      {
         loader.loadLanguage();
         loader._bLocalFileListLoaded = true;
      };
      _loc5_.onLoadError = function(mc)
      {
         loader.nonCriticalError(loader.getText("IMPOSSIBLE_TO_GET_FILE",[sURL]),loader.TABULATION + loader.TABULATION);
         loader.checkLocalFileListWithNextHost(sFiles);
      };
      _loc4_.addListener(_loc5_);
      this.log(this.TABULATION + this.getDataBankLogHeader(_loc3_.dataBankId) + this.getText("CHECKING_VERSIONS"));
      _loc4_.loadClip(sURL,this._mcLocalFileList);
   }
   function loadXtra()
   {
      this.clearlogs();
      this.showLoader(true);
      this.showBanner(true);
      this.showMainLogger(false);
      this.showShowLogsButton(false);
      this.showConfigurationChoice(false);
      this.showNextButton(false);
      this.showContinueButton(false);
      this.showClearCacheButton(false);
      this.showCopyLogsButton(false);
      this.showProgressBar(false);
      this.launchBannerAnim(true);
      this.setTotalBarValue(0,100);
      this.showBasicInformations();
      if(!this.checkOccurences())
      {
         this.showShowLogsButton(false);
         this.showCopyLogsButton(false);
         return undefined;
      }
      this.logTitle(this.getText("LOAD_XTRA_FILES"));
      this.log(this.TABULATION + this.getText("CHECK_LAST_VERSION"));
      this._sStep = "XTRA";
      this.moveProgressBar(-60);
      var _loc2_ = dofus.utils.Api.getInstance();
      if(_loc2_ != undefined)
      {
         _loc2_.lang.clearSOXtraCache();
      }
      var _loc3_ = this.copyAndOrganizeDataBanks();
      this._aCurrentDataBanks = _loc3_;
      this.showWaitBar(false);
      this._nTotalXtraFilesToLoad = 0;
      var _loc4_ = _global.API.lang.getConfigText("XTRA_FILE");
      var _loc5_ = 0;
      var _loc6_;
      var _loc7_;
      var _loc8_;
      var _loc9_;
      var _loc10_;
      var _loc11_;
      var _loc12_;
      while(_loc5_ < _loc3_.length)
      {
         _loc6_ = this.getXtraSharedObject(_loc5_);
         _global[dofus.Constants.GLOBAL_SO_XTRA_NAME + "_" + _loc5_] = _loc6_;
         _loc7_ = _loc6_.data.VERSIONS;
         _loc8_ = 0;
         while(_loc8_ < _loc4_.length)
         {
            _loc9_ = _loc4_[_loc8_];
            _loc10_ = _loc7_[_loc9_] != undefined ? _loc7_[_loc9_] : 0;
            _loc11_ = this._aXtraCurrentVersion[_loc5_];
            if(_loc11_ == undefined)
            {
               _loc11_ = [];
               this._aXtraCurrentVersion[_loc5_] = _loc11_;
            }
            _loc11_[_loc9_] = _loc10_;
            _loc8_ = _loc8_ + 1;
         }
         _loc12_ = this._aDistantFilesList[_loc5_].split("|");
         this._aXtraList[_loc5_] = _loc12_;
         this._nTotalXtraFilesToLoad += _loc12_.length;
         _loc5_ = _loc5_ + 1;
      }
      this._nRemainingXtraFilesToLoad = this._nTotalXtraFilesToLoad;
      var _loc13_ = 0;
      while(_loc13_ < _loc3_.length)
      {
         this.updateNextXtra(_loc13_);
         _loc13_ = _loc13_ + 1;
      }
   }
   function updateNextXtra(nDataBank)
   {
      var _loc3_ = this._aXtraList[nDataBank];
      var _loc4_ = this._aCurrentXtraLoadFile[nDataBank];
      if(this._bSkipDistantLoad && _loc4_ != undefined)
      {
         _loc3_.push(_loc4_);
      }
      var _loc5_;
      var _loc6_;
      var _loc7_;
      var _loc8_;
      var _loc9_;
      var _loc10_;
      var _loc11_;
      var _loc12_;
      var _loc13_;
      if(_loc3_.length < 1)
      {
         this.noMoreXtra();
      }
      else
      {
         while(true)
         {
            if(_loc3_.length > 0)
            {
               this.setTotalBarValue(10 + (90 - 90 / this._nTotalXtraFilesToLoad * this._nRemainingXtraFilesToLoad),100);
               this._nRemainingXtraFilesToLoad = this._nRemainingXtraFilesToLoad - 1;
               _loc5_ = _loc3_.shift().split(",");
               this._aCurrentXtra[nDataBank] = _loc5_;
               if(_loc3_.length > 0 && _loc5_[2])
               {
                  if(!this._bSkipDistantLoad)
                  {
                     this._aCurrentXtraLoadFile[nDataBank] = _loc5_;
                  }
                  _loc6_ = _loc5_[0];
                  _loc7_ = _loc5_[1];
                  _loc8_ = _loc5_[2];
                  if(_loc6_ != "lang")
                  {
                     this._mcProgressBarGroup.txtInfo.text = _loc6_;
                     _loc9_ = this._aXtraCurrentVersion[nDataBank][_loc6_];
                     if(!(_global.CONFIG.language == this.getLangSharedObject(nDataBank).data.LANGUAGE && Number(_loc8_) == _loc9_))
                     {
                        if(!this._bLocalFileListLoaded)
                        {
                           if(this._bSkipDistantLoad)
                           {
                              return undefined;
                           }
                           addr7614:
                           this._bUpdate = true;
                           _loc5_[3] = _loc5_[0] + "_" + _loc5_[1] + "_" + _loc5_[2];
                           this.log(this.TABULATION + this.getDataBankLogHeader(nDataBank) + this.getText("UPDATE_FILE",[_loc6_]));
                           this.showWaitBar(true);
                           _loc10_ = new dofus.utils.LangFileLoader();
                           _loc10_.addListener(this);
                           if(dofus.Constants.USE_JS_LOG && _global.CONFIG.isNewAccount)
                           {
                              this.getURL("JavaScript:WriteLog(\'updateNextXtra;" + _loc6_ + "_" + _global.CONFIG.language + "_" + _loc8_ + "\')");
                           }
                           _loc11_ = dofus.Constants.XTRA_SHAREDOBJECT_NAME + "_" + nDataBank;
                           _loc12_ = this.copyAndOrganizeDataServersForDataBank(nDataBank);
                           _loc13_ = this.getDataBankMcContainer(nDataBank);
                           _loc10_.loadLangFile(_loc12_,"lang/swf/" + _loc6_ + "_" + _global.CONFIG.language + "_" + _loc8_ + ".swf",_loc13_,_loc11_,_loc6_,_global.CONFIG.language,true);
                           return undefined;
                        }
                        if(!this._bSkipDistantLoad)
                        {
                           break;
                        }
                        if(this._aXtraCurrentVersion[nDataBank][_loc6_] == 0)
                        {
                           _loc8_ = this._mcLocalFileList.VERSIONS[_global.CONFIG.language][_loc6_];
                           break;
                        }
                     }
                  }
               }
            }
            else
            {
               this.noMoreXtra();
               return;
            }
         }
         §§goto(addr7614);
      }
   }
   function noMoreXtra()
   {
      var _loc2_ = true;
      var _loc3_ = 0;
      var _loc4_;
      while(_loc3_ < this._aXtraList.length)
      {
         _loc4_ = this._aXtraList[_loc3_];
         if(_loc4_ != undefined && _loc4_.length > 0)
         {
            _loc2_ = false;
            break;
         }
         _loc3_ = _loc3_ + 1;
      }
      if(!_loc2_)
      {
         return undefined;
      }
      this.logTitle(this.getText("INIT_END"));
      if(dofus.Constants.USE_JS_LOG && _global.CONFIG.isNewAccount)
      {
         this.getURL("JavaScript:WriteLog(\'XtraLangLoadEnd\')");
      }
      if((this._bNonCriticalError || this._bUpdate) && (dofus.Constants.DEBUG && (Key.isDown(Key.SHIFT) && dofus.Kernel.FAST_SWITCHING_SERVER_REQUEST == undefined)))
      {
         this.showBanner(false);
         this.showMainLogger();
         this.showNextButton(true);
         this.showCopyLogsButton(true);
         this.showShowLogsButton(true);
      }
      else
      {
         this.initAndLoginFinished();
      }
   }
   function initAndLoginFinished()
   {
      this.showLoader(false);
      _global.API.kernel.onInitAndLoginFinished();
      this._bNonCriticalError = false;
      this._bUpdate = false;
      this.launchBannerAnim(false);
      this.showBanner(false);
   }
   function checkCacheVersion()
   {
      var _loc2_ = new Date();
      var _loc3_ = _loc2_.getFullYear() + "-" + (_loc2_.getMonth() + 1) + "-" + _loc2_.getDate();
      if(!this.getCacheDateSharedObject().data.clearDate)
      {
         this.clearCache();
         this.getCacheDateSharedObject().data.clearDate = _loc3_;
         this.getCacheDateSharedObject().flush(100);
         return false;
      }
      var _loc4_ = _global[dofus.Constants.GLOBAL_SO_LANG_NAME + "_" + dofus.utils.DofusTranslator.STANDARD_DATA_BANK];
      if(_loc4_ && (_loc4_.data.C.CLEAR_DATE && _loc4_.data.C.ENABLED_AUTO_CLEARCACHE))
      {
         if(this.getCacheDateSharedObject().data.clearDate < _loc4_.data.C.CLEAR_DATE)
         {
            this.clearCache();
            this.getCacheDateSharedObject().data.clearDate = _loc4_.data.C.CLEAR_DATE;
            this.getCacheDateSharedObject().flush();
            this.reboot();
            return false;
         }
      }
      return true;
   }
   function onLoadStart(mc)
   {
      this.showWaitBar(false);
      this.setProgressBarValue(0,100);
   }
   function onTimedProgress(shit, ldr, target)
   {
      var _loc5_ = ldr.getProgress(target);
      shit.setProgressBarValue(Number(_loc5_.bytesLoaded),Number(_loc5_.bytesTotal));
   }
   function onLoadError(mc, errorCode, httpStatus, oServer)
   {
      _global.clearInterval(this._timedProgress);
      this.showProgressBar(false);
      this.showWaitBar(false);
      var _loc6_ = oServer.dataBankId;
      var _loc7_;
      switch(this._sStep)
      {
         case "LANG":
            if(oServer.type == "local")
            {
               this.log(this.TABULATION + this.TABULATION + this.getDataBankLogHeader(_loc6_) + this.getText("NO_FILE_IN_LOCAL",["lang",oServer.url]));
            }
            else
            {
               if(dofus.Constants.USE_JS_LOG && _global.CONFIG.isNewAccount)
               {
                  this.getURL("JavaScript:WriteLog(\'onLoadError LANG-" + oServer.url + "lang" + "\')");
               }
               this.nonCriticalError(this.getText("IMPOSSIBLE_TO_DOWNLOAD_FILE",["lang",oServer.url]),this.TABULATION + this.TABULATION + this.getDataBankLogHeader(_loc6_));
            }
            break;
         case "MODULE":
            if(dofus.Constants.USE_JS_LOG && _global.CONFIG.isNewAccount)
            {
               this.getURL("JavaScript:WriteLog(\'onLoadError MODULE-" + this._aCurrentModule[4] + "\')");
            }
            this.criticalError("IMPOSSIBLE_TO_LOAD_MODULE",this.TABULATION,true,[this._aCurrentModule[4]]);
            break;
         case "XTRA":
            _loc7_ = this._aCurrentXtra[_loc6_];
            if(oServer.type == "local")
            {
               this.log(this.TABULATION + this.TABULATION + this.getDataBankLogHeader(_loc6_) + this.getText("NO_FILE_IN_LOCAL",[_loc7_[3],oServer.url]));
            }
            else
            {
               if(dofus.Constants.USE_JS_LOG && _global.CONFIG.isNewAccount)
               {
                  this.getURL("JavaScript:WriteLog(\'onLoadError XTRA-" + oServer.url + _loc7_[3] + "\')");
               }
               this.nonCriticalError(this.getText("IMPOSSIBLE_TO_DOWNLOAD_FILE",[_loc7_[3],oServer.url]),this.TABULATION + this.TABULATION + this.getDataBankLogHeader(_loc6_));
            }
         default:
            return;
      }
   }
   function onLoadComplete(mc)
   {
      _global.clearInterval(this._timedProgress);
      if(this._sStep == "MODULE")
      {
         _global["MODULE_" + this._aCurrentModule[4]] = mc;
      }
   }
   function onLoadInit(mc, oServer)
   {
      this.showProgressBar(false);
      var _loc4_ = oServer.dataBankId;
      var _loc5_;
      switch(this._sStep)
      {
         case "LANG":
            this._nLoadedLangFiles = this._nLoadedLangFiles + 1;
            this.logGreen(this.TABULATION + this.getDataBankLogHeader(_loc4_) + this.getText("UPDATE_FINISH",["lang",oServer.url]));
            if(this._aCurrentDataBanks.length == this._nLoadedLangFiles)
            {
               if(!this.checkCacheVersion())
               {
                  return undefined;
               }
               this.loadModules();
            }
            break;
         case "MODULE":
            this.log(this.TABULATION + this.getText("MODULE_LOADED",[this._aCurrentModule[4]]));
            if(!this.checkCacheVersion())
            {
               return undefined;
            }
            this.loadNextModule();
            break;
         case "XTRA":
            _loc5_ = this._aCurrentXtra[_loc4_];
            if(oServer.type == "local")
            {
               this.logGreen(this.TABULATION + this.TABULATION + this.getDataBankLogHeader(_loc4_) + this.getText("FILE_LOADED",[_loc5_[3],oServer.url]));
            }
            else
            {
               this.logGreen(this.TABULATION + this.TABULATION + this.getDataBankLogHeader(_loc4_) + this.getText("UPDATE_FINISH",[_loc5_[3],oServer.url]));
            }
            this._aCurrentXtraLoadFile[_loc4_] = undefined;
            this.updateNextXtra(_loc4_);
         default:
            return;
      }
   }
   function onCorruptFile(mc, totalBytes, oServer)
   {
      switch(this._sStep)
      {
         case "LANG":
            this.nonCriticalError(this.getText("CORRUPT_FILE",["lang",oServer.url,totalBytes]),this.TABULATION + this.TABULATION);
            break;
         case "XTRA":
            this.nonCriticalError(this.getText("CORRUPT_FILE",[this._aCurrentXtra[3],oServer.url,totalBytes]),this.TABULATION + this.TABULATION);
         default:
            return;
      }
   }
   function onCantWrite(mc)
   {
      switch(this._sStep)
      {
         case "LANG":
            this.criticalError("WRITE_FAILED",this.TABULATION + this.TABULATION,true,["lang"]);
            break;
         case "XTRA":
            this.criticalError("WRITE_FAILED",this.TABULATION + this.TABULATION,true,[this._aCurrentXtra[3]]);
         default:
            return;
      }
   }
   function onAllLoadFailed(mc)
   {
      this.showProgressBar(false);
      this.showWaitBar(false);
      if(dofus.Constants.USE_JS_LOG && _global.CONFIG.isNewAccount)
      {
         this.getURL("JavaScript:WriteLog(\'onAllLoadFailed;" + this._sStep + "\')");
      }
      switch(this._sStep)
      {
         case "LANG":
            if(!this._bSkipDistantLoad)
            {
               this.criticalError("CANT_UPDATE_FILE",this.TABULATION + this.TABULATION,true,["lang"]);
            }
            else
            {
               this.nonCriticalError("CANT_UPDATE_FILE",this.TABULATION + this.TABULATION,true,["lang"]);
            }
            this._bSkipDistantLoad = true;
            break;
         case "XTRA":
            this._bSkipDistantLoad = true;
            this.nonCriticalError("CANT_UPDATE_FILE",this.TABULATION + this.TABULATION,true,[this._aCurrentXtra[3]]);
            this.updateNextXtra();
         default:
            return;
      }
   }
   function onCoreDisplayed()
   {
      this.launchBannerAnim(false);
      this.showBanner(false);
      this.showLoader(false);
   }
}
