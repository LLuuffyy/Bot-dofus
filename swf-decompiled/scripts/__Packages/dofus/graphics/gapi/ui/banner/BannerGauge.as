class dofus.graphics.gapi.ui.banner.BannerGauge
{
   function BannerGauge()
   {
   }
   static function showGaugeMode(mcBanner)
   {
      if(mcBanner.api.datacenter.Player.XP == undefined || mcBanner.api.datacenter.Game.isFight)
      {
         return undefined;
      }
      var _loc3_ = mcBanner.api.kernel.OptionsManager.getOption("BannerGaugeMode");
      if(mcBanner._mcCurrentXtraMask == mcBanner._mcCircleXtraMaskBig || _loc3_ == "none")
      {
         mcBanner.circleXtra.setXtraFightMask(false);
         return undefined;
      }
      mcBanner.circleXtra.setXtraFightMask(true);
      var _loc4_;
      var _loc5_;
      var _loc6_;
      var _loc7_;
      var _loc8_;
      var _loc9_;
      var _loc10_;
      switch(_loc3_)
      {
         case "xp":
            _loc4_ = mcBanner.api.datacenter.Player.XPhigh - mcBanner.api.datacenter.Player.XPlow;
            if(_loc4_ <= 0)
            {
               _loc5_ = 100;
            }
            else
            {
               _loc5_ = Math.floor((mcBanner.api.datacenter.Player.XP - mcBanner.api.datacenter.Player.XPlow) / _loc4_ * 100);
            }
            _loc6_ = 8298148;
            break;
         case "xpmount":
            if(mcBanner.api.datacenter.Player.mount == undefined)
            {
               _loc5_ = 0;
            }
            else
            {
               _loc5_ = Math.floor((mcBanner.api.datacenter.Player.mount.xp - mcBanner.api.datacenter.Player.mount.xpMin) / (mcBanner.api.datacenter.Player.mount.xpMax - mcBanner.api.datacenter.Player.mount.xpMin) * 100);
            }
            _loc6_ = 8298148;
            break;
         case "pods":
            _loc5_ = Math.floor(mcBanner.api.datacenter.Player.currentWeight / mcBanner.api.datacenter.Player.maxWeight * 100);
            _loc6_ = 6340148;
            break;
         case "energy":
            if(mcBanner.api.datacenter.Player.EnergyMax == -1)
            {
               _loc5_ = 0;
            }
            else
            {
               _loc5_ = Math.floor(mcBanner.api.datacenter.Player.Energy / mcBanner.api.datacenter.Player.EnergyMax * 100);
            }
            _loc6_ = 10994717;
            break;
         case "xpcurrentjob":
            _loc7_ = mcBanner.api.datacenter.Player.currentJobID;
            if(_loc7_ != undefined)
            {
               _loc8_ = mcBanner.api.datacenter.Player.Jobs.findFirstItem("id",_loc7_).item;
               if(_loc8_.xpMax != -1)
               {
                  _loc5_ = Math.floor((_loc8_.xp - _loc8_.xpMin) / (_loc8_.xpMax - _loc8_.xpMin) * 100);
               }
               else
               {
                  _loc5_ = 0;
               }
            }
            else
            {
               _loc5_ = 0;
            }
            _loc6_ = 10441125;
            break;
         case "tempotons":
            _loc9_ = mcBanner.api.datacenter.Player.Inventory.findFirstItem("unicID",dofus.graphics.gapi.controls.temporis.TemporisGeneral.TEMPOTON_ID);
            _loc10_ = _loc9_.item;
            _loc5_ = _loc10_.Quantity / Number(mcBanner.api.lang.getConfigText("TEMPORIS_3_TEMPOTONS_MAX_COUNT")) * 100;
            _loc6_ = 16239703;
      }
      if(!_global.isNaN(_loc6_))
      {
         if(_global.isNaN(_loc5_))
         {
            _loc5_ = 0;
         }
         mcBanner._ccChrono._visible = true;
         mcBanner._ccChrono.setGaugeChrono(_loc5_,_loc6_);
      }
   }
   static function setGaugeMode(mcBanner, sGaugeMode)
   {
      mcBanner._mcCurrentXtraMask = sGaugeMode != "none" ? mcBanner._mcCircleXtraMask : mcBanner._mcCircleXtraMaskBig;
      var _loc3_ = mcBanner.api.kernel.OptionsManager.getOption("BannerGaugeMode");
      switch(_loc3_)
      {
         case "xp":
            mcBanner.api.datacenter.Player.removeEventListener("xpChanged",mcBanner);
            break;
         case "xpmount":
            mcBanner.api.datacenter.Player.removeEventListener("mountChanged",mcBanner);
            break;
         case "pods":
            mcBanner.api.datacenter.Player.removeEventListener("currentWeightChanged",mcBanner);
            break;
         case "energy":
            mcBanner.api.datacenter.Player.removeEventListener("energyChanged",mcBanner);
            break;
         case "xpcurrentjob":
            mcBanner.api.datacenter.Player.removeEventListener("currentJobChanged",mcBanner);
            break;
         case "tempotons":
            mcBanner.api.datacenter.Player.removeEventListener("tempotonsChanged",mcBanner);
      }
      mcBanner.api.kernel.OptionsManager.setOption("BannerGaugeMode",sGaugeMode);
      switch(sGaugeMode)
      {
         case "xp":
            mcBanner.api.datacenter.Player.addEventListener("xpChanged",mcBanner);
            break;
         case "xpmount":
            mcBanner.api.datacenter.Player.addEventListener("mountChanged",mcBanner);
            break;
         case "pods":
            mcBanner.api.datacenter.Player.addEventListener("currentWeightChanged",mcBanner);
            break;
         case "energy":
            mcBanner.api.datacenter.Player.addEventListener("energyChanged",mcBanner);
            break;
         case "xpcurrentjob":
            mcBanner.api.datacenter.Player.addEventListener("currentJobChanged",mcBanner);
            break;
         case "tempotons":
            mcBanner.api.datacenter.Player.addEventListener("tempotonsChanged",mcBanner);
      }
      dofus.graphics.gapi.ui.banner.BannerGauge.showGaugeMode(mcBanner);
   }
   static function showGaugeModeSelectMenu(mcBanner)
   {
      var _loc4_ = mcBanner.api.kernel.OptionsManager.getOption("BannerGaugeMode");
      var _loc5_ = mcBanner.api.ui.createPopupMenu();
      _loc5_.addItem(mcBanner.api.lang.getText("DISABLE"),dofus.graphics.gapi.ui.banner.BannerGauge,dofus.graphics.gapi.ui.banner.BannerGauge.setGaugeMode,[mcBanner,"none"],_loc4_ != "none");
      _loc5_.addItem(mcBanner.api.lang.getText("WORD_XP"),dofus.graphics.gapi.ui.banner.BannerGauge,dofus.graphics.gapi.ui.banner.BannerGauge.setGaugeMode,[mcBanner,"xp"],_loc4_ != "xp");
      _loc5_.addItem(mcBanner.api.lang.getText("WORD_XP") + " " + mcBanner.api.lang.getText("JOB"),dofus.graphics.gapi.ui.banner.BannerGauge,dofus.graphics.gapi.ui.banner.BannerGauge.setGaugeMode,[mcBanner,"xpcurrentjob"],_loc4_ != "xpcurrentjob");
      _loc5_.addItem(mcBanner.api.lang.getText("WORD_XP") + " " + mcBanner.api.lang.getText("MOUNT"),dofus.graphics.gapi.ui.banner.BannerGauge,dofus.graphics.gapi.ui.banner.BannerGauge.setGaugeMode,[mcBanner,"xpmount"],_loc4_ != "xpmount");
      _loc5_.addItem(mcBanner.api.lang.getText("WEIGHT"),dofus.graphics.gapi.ui.banner.BannerGauge,dofus.graphics.gapi.ui.banner.BannerGauge.setGaugeMode,[mcBanner,"pods"],_loc4_ != "pods");
      _loc5_.addItem(mcBanner.api.lang.getText("ENERGY"),dofus.graphics.gapi.ui.banner.BannerGauge,dofus.graphics.gapi.ui.banner.BannerGauge.setGaugeMode,[mcBanner,"energy"],_loc4_ != "energy");
      if(_global.API.datacenter.Basics.aks_current_server.isTemporis())
      {
         _loc5_.addItem(mcBanner.api.lang.getText("TEMPOTONS"),dofus.graphics.gapi.ui.banner.BannerGauge,dofus.graphics.gapi.ui.banner.BannerGauge.setGaugeMode,[mcBanner,"tempotons"],_loc4_ != "tempotons");
      }
      _loc5_.show(_root._xmouse,_root._ymouse,true);
   }
}
