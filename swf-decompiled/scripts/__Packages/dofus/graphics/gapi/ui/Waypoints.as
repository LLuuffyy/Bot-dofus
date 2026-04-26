class dofus.graphics.gapi.ui.Waypoints extends dofus.graphics.gapi.core.DofusAdvancedComponent
{
   var _btnClose;
   var _btnClose2;
   var _btnTeleport;
   var _eaData;
   var _eaDataCopy;
   var _lblArea;
   var _lblCoords;
   var _lblCost;
   var _lblDescription;
   var _lblName;
   var _lstWaypoints;
   var _oSelectedRow;
   var _tiSearch;
   var _winBg;
   var addToQueue;
   var buscar;
   var initialized;
   static var CLASS_NAME = "Waypoints";
   function Waypoints()
   {
      super();
   }
   function set data(eaData)
   {
      this.addToQueue({object:this,method:function(d)
      {
         this._eaData = d;
         if(this.initialized)
         {
            this.initData();
         }
      },params:[eaData]});
   }
   function init()
   {
      super.init(false,dofus.graphics.gapi.ui.Waypoints.CLASS_NAME);
   }
   function callClose()
   {
      this.api.network.Waypoints.leave();
      return true;
   }
   function createChildren()
   {
      this.addToQueue({object:this,method:this.initTexts});
      this.addToQueue({object:this,method:this.addListeners});
      this.addToQueue({object:this,method:this.initData});
   }
   function initTexts()
   {
      this._winBg.title = this.api.lang.getText("WAYPOINT_LIST");
      this._lblCoords.text = this.api.lang.getText("COORDINATES_SMALL");
      this._lblName.text = this.api.lang.getText("SUBAREA") + " (" + this.api.lang.getText("RESPAWN_SMALL") + ")";
      this._lblCost.text = this.api.lang.getText("COST");
      this._lblArea.text = this.api.lang.getText("AREA");
      this._lblDescription.text = this.api.lang.getText("CLICK_ON_WAYPOINT");
      this._btnClose2.label = this.api.lang.getText("CLOSE");
      this._btnTeleport.label = this.api.lang.getText("TELEPORT");
      this._tiSearch.text = "";
   }
   function addListeners()
   {
      this._btnClose.addEventListener("click",this);
      this._btnClose2.addEventListener("click",this);
      this._btnTeleport.addEventListener("click",this);
      this._lstWaypoints.addEventListener("itemdblClick",this);
      this._lstWaypoints.addEventListener("itemSelected",this);
      this._tiSearch.addEventListener("change",this);
   }
   function change(oEvent)
   {
      var _loc3_ = this._tiSearch.text;
      if(_loc3_.length >= 3)
      {
         this.buscar(_loc3_.toUpperCase());
      }
      else
      {
         this._lstWaypoints.dataProvider = this._eaDataCopy;
      }
   }
   function searchItem(sText)
   {
      var _loc3_ = sText.split(" ");
      var _loc4_ = new ank.utils.ExtendedArray();
      var _loc5_ = 0;
      var _loc6_;
      var _loc7_;
      while(_loc5_ < this._eaDataCopy.length)
      {
         _loc6_ = this._eaDataCopy[_loc5_];
         _loc7_ = (_loc6_.subareaName + " " + _loc6_.areaName + " " + _loc6_.coordinates).toUpperCase();
         if(this.searchWordsInName(_loc3_,_loc7_,0) > 0)
         {
            _loc4_.push(_loc6_);
         }
         _loc5_ += 1;
      }
      this._lstWaypoints.dataProvider = _loc4_;
   }
   function cloneWaypoint(o)
   {
      var _loc2_ = {};
      for(var _loc3_ in o)
      {
         _loc2_[_loc3_] = o[_loc3_];
      }
      if(_loc2_.cost == undefined)
      {
         _loc2_.cost = 0;
      }
      if(_loc2_.coordinates == undefined)
      {
         _loc2_.coordinates = "";
      }
      if(_loc2_.areaName == undefined)
      {
         _loc2_.areaName = "";
      }
      if(_loc2_.subareaName == undefined)
      {
         _loc2_.subareaName = "";
      }
      if(_loc2_.isRespawn == undefined)
      {
         _loc2_.isRespawn = false;
      }
      if(_loc2_.isCurrent == undefined)
      {
         _loc2_.isCurrent = false;
      }
      return _loc2_;
   }
   function searchWordsInName(aWords, sName, nMaxWordsCount)
   {
      var _loc4_ = 0;
      var _loc5_ = aWords.length;
      var _loc6_;
      while(_loc5_ >= 0)
      {
         _loc6_ = aWords[_loc5_];
         if(sName.indexOf(_loc6_) != -1)
         {
            _loc4_ += 1;
         }
         else if(_loc4_ + _loc5_ < nMaxWordsCount)
         {
            return 0;
         }
         _loc5_ -= 1;
      }
      return _loc4_;
   }
   function initData()
   {
      var _loc2_;
      if(this._eaData != undefined)
      {
         this._eaData.sortOn("fieldToSort",Array.CASEINSENSITIVE);
         this._eaDataCopy = new ank.utils.ExtendedArray();
         _loc2_ = 0;
         while(_loc2_ < this._eaData.length)
         {
            this._eaDataCopy.push(this._eaData[_loc2_]);
            _loc2_ += 1;
         }
         this._lstWaypoints.dataProvider = this._eaData;
      }
   }
   function teleport()
   {
      var _loc2_ = this._oSelectedRow;
      var _loc3_ = _loc2_.cost;
      if(this.api.datacenter.Player.Kama >= _loc3_)
      {
         this.api.network.Waypoints.use(_loc2_.id);
      }
      else
      {
         this.api.kernel.showMessage(undefined,this.api.lang.getText("NOT_ENOUGH_RICH"),"ERROR_CHAT");
      }
   }
   function click(oEvent)
   {
      switch(oEvent.target)
      {
         case this._btnClose:
         case this._btnClose2:
            this.callClose();
            return undefined;
         case this._btnTeleport:
            this.teleport();
      }
      return undefined;
   }
   function itemdblClick(oEvent)
   {
      this.teleport();
   }
   function itemSelected(oEvent)
   {
      this._oSelectedRow = oEvent.row.item;
      this._btnTeleport.enabled = true;
   }
}
