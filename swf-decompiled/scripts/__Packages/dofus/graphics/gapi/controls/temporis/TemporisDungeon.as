class dofus.graphics.gapi.controls.temporis.TemporisDungeon extends dofus.graphics.gapi.core.DofusAdvancedComponent
{
   var _lblCursedDungeons;
   var _lblTime;
   var _listDungeons;
   var _listRanking;
   var _mcAboutDungeonRanking;
   var _mcPlayerInfo;
   var _taTextWaiting;
   var addToQueue;
   var api;
   var gapi;
   static var CLASS_NAME = "TemporisDungeons";
   var nRushu = 36;
   function TemporisDungeon()
   {
      super();
   }
   function init()
   {
      super.init(false,dofus.graphics.gapi.controls.temporis.TemporisDungeon.CLASS_NAME);
   }
   function createChildren()
   {
      this.addToQueue({object:this,method:this.addListeners});
      this.addToQueue({object:this,method:this.initText});
      this.addToQueue({object:this,method:this.initData});
   }
   function initText()
   {
      this._listRanking.columnsNames = ["",this.api.lang.getText("RANKING"),this.api.lang.getText("NAME_BIG"),this.api.lang.getText("NB_GLOBAL_TURNS")];
   }
   function initData()
   {
      this._listDungeons.dataProvider = this.api.datacenter.Temporis.dungeons;
      this.unSelectDungeon();
   }
   function addListeners()
   {
      this._listDungeons.addEventListener("itemSelected",this);
      var that = this;
      this._mcAboutDungeonRanking.onRollOver = function()
      {
         that.titleRollOver();
      };
      this._mcAboutDungeonRanking.onRollOut = function()
      {
         that.titleRollOut();
      };
   }
   function unSelectDungeon()
   {
      this._listDungeons.selectedIndex = -1;
      this._listRanking._visible = false;
      this._listRanking.dataProvider = undefined;
      this._taTextWaiting.text = this.api.lang.getText("TR3_DUNGEON_TXT");
      this._taTextWaiting._visible = true;
      this._lblCursedDungeons.text = this.api.lang.getText("TR3_CHOOSE_DUNGEON");
      this._lblTime.text = this.api.lang.getText("TR3_DUNGEON_LADDER_TIME",this.api.datacenter.Temporis.dungeonLadderDate);
      this._mcPlayerInfo._visible = false;
   }
   function selectDungeon(nDungeonId)
   {
      if(nDungeonId == this.nRushu)
      {
         this._listRanking._visible = false;
         this._listRanking.dataProvider = undefined;
         this._taTextWaiting.text = this.api.lang.getText("TR3_DUNGEON_TXT");
         this._taTextWaiting._visible = true;
         this._mcPlayerInfo._visible = false;
      }
      this.api.network.Temporis.episodeThree.askDungeonLadderRank(nDungeonId);
   }
   function updateRanking()
   {
      var _loc2_ = this.api.datacenter.Temporis.dungeonPlayerInfo;
      this._mcPlayerInfo.setValue(true,undefined,_loc2_);
      this._mcPlayerInfo._visible = true;
      this._taTextWaiting._visible = false;
      this._lblCursedDungeons._visible = false;
      this._lblTime._visible = false;
      if(this._listDungeons.selectedItem.id == this.nRushu)
      {
         this._listRanking.columnsNames = ["",this.api.lang.getText("RANKING"),this.api.lang.getText("NAME_BIG"),this.api.lang.getText("SCORE")];
      }
      else
      {
         this.initText();
      }
      this._listRanking.dataProvider = this.api.datacenter.Temporis.dungeonRanks;
      this._listRanking._visible = true;
   }
   function itemSelected(oEvent)
   {
      var _loc3_ = oEvent.row.item;
      this.selectDungeon(Number(_loc3_.id));
   }
   function titleRollOver(oEvent)
   {
      if(this._listDungeons.selectedItem.id == this.nRushu && this._mcPlayerInfo._visible)
      {
         this.gapi.showTooltip(this.api.lang.getText("RUSHU_RANKING_ABOUT"),this._mcAboutDungeonRanking,20);
      }
      else
      {
         this.gapi.showTooltip(this.api.lang.getText("DUNGEONS_RANKING_ABOUT"),this._mcAboutDungeonRanking,20);
      }
   }
   function titleRollOut(oEvent)
   {
      this.gapi.hideTooltip();
   }
}
