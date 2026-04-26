class dofus.graphics.gapi.ui.temporis3.TemporisInvadeRankItem extends ank.gapi.core.UIBasicComponent
{
   var _lblName;
   var _lblRank;
   var _lblScore;
   var _ldrBreed;
   var _mcList;
   var _oItem;
   var addToQueue;
   function TemporisInvadeRankItem()
   {
      super();
      this.initialize();
   }
   function set list(mcList)
   {
      this._mcList = mcList;
   }
   function get list()
   {
      return this._mcList;
   }
   function initialize()
   {
   }
   function setValue(bUsed, sSuggested, oItem)
   {
      this._oItem = oItem;
      this.createChildren();
   }
   function init()
   {
      super.init(false);
   }
   function createChildren()
   {
      this.addToQueue({object:this,method:this.initTexts});
      this.addToQueue({object:this,method:this.initData});
   }
   function initTexts()
   {
      if(this._oItem == undefined)
      {
         this._lblRank.text = "-";
         this._lblName.text = "-";
         this._lblScore.text = "-";
      }
      else
      {
         this._lblRank.text = this._oItem.rank;
         this._lblName.text = this._oItem.name;
         this._lblScore.text = this._oItem.score;
      }
   }
   function initData()
   {
      if(this._oItem == undefined)
      {
         this._ldrBreed.contentPath = "";
      }
      else
      {
         this._ldrBreed.contentPath = dofus.Constants.GUILDS_MINI_PATH + this._oItem.breed + ".swf";
      }
   }
}
