class dofus.graphics.gapi.controls.temporis.TemporisInvades extends dofus.graphics.gapi.core.DofusAdvancedComponent
{
   var _lblInvadeAreas;
   var _listAreas;
   var _listRanking;
   var _mcAboutInvadeRanking;
   var _mcPlayerInfo;
   var _taTextWaiting;
   var addToQueue;
   var api;
   var gapi;
   static var CLASS_NAME = "TemporisInvades";
   function TemporisInvades()
   {
      super();
   }
   function init()
   {
      super.init(false,dofus.graphics.gapi.controls.temporis.TemporisInvades.CLASS_NAME);
   }
   function createChildren()
   {
      this.addToQueue({object:this,method:this.addListeners});
      this.addToQueue({object:this,method:this.initText});
      this.addToQueue({object:this,method:this.initData});
   }
   function initText()
   {
      this._listRanking.columnsNames = ["",this.api.lang.getText("RANKING"),this.api.lang.getText("NAME_BIG"),this.api.lang.getText("SCORE")];
   }
   function initData()
   {
      this._listAreas.dataProvider = this.api.datacenter.Temporis.invadeAreas;
      this.unSelectArea();
      var _loc2_ = this.api.datacenter.Temporis.actualInvade;
      if(_loc2_ != -1)
      {
         this._listAreas.selectedIndex = _loc2_ - 1;
         this.selectArea(_loc2_);
      }
   }
   function addListeners()
   {
      this._listAreas.addEventListener("itemSelected",this);
      var that = this;
      this._mcAboutInvadeRanking.onRollOver = function()
      {
         that.titleRollOver();
      };
      this._mcAboutInvadeRanking.onRollOut = function()
      {
         that.titleRollOut();
      };
   }
   function unSelectArea()
   {
      this._listAreas.selectedIndex = -1;
      this._listRanking._visible = false;
      this._listRanking.dataProvider = undefined;
      this._mcPlayerInfo._visible = false;
      this._taTextWaiting.text = this.api.lang.getText("TR3_INVADE_TXT");
      this._lblInvadeAreas.text = this.api.lang.getText("TR3_CHOOSE_INVADE");
   }
   function selectArea(nInvadeId)
   {
      this.api.network.Temporis.episodeThree.askInvadeRank(nInvadeId);
   }
   function updateRanking()
   {
      var _loc2_ = this.api.datacenter.Temporis.invadePlayerInfo;
      this._mcPlayerInfo.setValue(true,undefined,_loc2_);
      this._mcPlayerInfo._visible = true;
      this._taTextWaiting._visible = false;
      this._lblInvadeAreas._visible = false;
      this._listRanking.dataProvider = this.api.datacenter.Temporis.invadeRanks;
      this._listRanking._visible = true;
   }
   function itemSelected(oEvent)
   {
      var _loc3_ = oEvent.row.item;
      this.selectArea(Number(_loc3_.id));
   }
   function titleRollOver(oEvent)
   {
      this.gapi.showTooltip(this.api.lang.getText("INVADES_RANKING_ABOUT"),this._mcAboutInvadeRanking,20);
   }
   function titleRollOut(oEvent)
   {
      this.gapi.hideTooltip();
   }
}
