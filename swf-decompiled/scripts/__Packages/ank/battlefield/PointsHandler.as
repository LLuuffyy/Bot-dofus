class ank.battlefield.PointsHandler
{
   var _incIndex;
   var _mcBattlefield;
   var _mcContainer;
   var _oDatacenter;
   var _oList;
   static var MAX_INDEX = 200;
   function PointsHandler(b, c, d)
   {
      this.initialize(b,c,d);
   }
   function initialize(b, c, d)
   {
      this._mcBattlefield = b;
      this._mcContainer = c;
      this._oDatacenter = d;
      this._oList = {};
      this._incIndex = 0;
   }
   function clear()
   {
      for(var k in this._mcContainer)
      {
         this._mcContainer[k].removeMovieClip();
      }
   }
   function addPoints(sID, nX, nY, sValue, nType)
   {
      var _loc7_ = this.getNextIndex();
      var _loc8_ = this._mcContainer.getNextHighestDepth();
      this._mcContainer.createEmptyMovieClip("pt" + _loc7_,_loc8_);
      var _loc9_ = this._mcContainer["pt" + _loc7_];
      var _loc10_ = _loc9_.createEmptyMovieClip("clip-pt" + _loc7_,_loc8_);
      _loc9_._x = nX;
      _loc9_._y = nY;
      _loc9_.mc = _loc10_;
      _loc9_.file = dofus.Constants.getPointClip(nType);
      _loc9_.value = sValue;
      _loc9_.sID = sID;
      _loc9_.thisPath = this;
      if(this._oList[sID] == undefined)
      {
         this._oList[sID] = [];
      }
      this._oList[sID].push(_loc9_);
      if(this._oList[sID].length == 1)
      {
         this.loadPointClip(_loc9_);
      }
   }
   function getNextIndex(Void)
   {
      this._incIndex = this._incIndex + 1;
      if(this._incIndex > ank.battlefield.PointsHandler.MAX_INDEX)
      {
         this._incIndex = 0;
      }
      return this._incIndex;
   }
   function loadPointClip(oPoint)
   {
      var _loc3_ = new MovieClipLoader();
      _loc3_.loadClip(oPoint.file,oPoint.mc);
   }
   function onAnimateFinished(sID)
   {
      var _loc3_ = this._oList[sID];
      _loc3_.shift();
      var _loc4_;
      if(_loc3_.length != 0)
      {
         _loc4_ = _loc3_[0];
         this.loadPointClip(_loc4_);
      }
   }
}
