class dofus.datacenter.MonsterGroup extends ank.battlefield.datacenter.Sprite
{
   var _aLevelsList;
   var _aNamesList;
   var _aTemplateIds;
   var _nBonusValue;
   var _sRawTemplateIds;
   var api;
   var _sDefaultAnimation = "static";
   var _bAllDirections = false;
   var _bForceWalk = true;
   var _nAlignmentIndex = -1;
   function MonsterGroup(sID, clipClass, sGfxFile, cellNum, dir, bonus)
   {
      super();
      this.api = _global.API;
      this._nBonusValue = bonus;
      this.initialize(sID,clipClass,sGfxFile,cellNum,dir,null);
   }
   function set name(value)
   {
      this._sRawTemplateIds = value;
      this._aTemplateIds = [];
      this._aNamesList = [];
      var _loc4_ = value.split(",");
      var _loc5_ = 0;
      var _loc6_;
      var _loc7_;
      while(_loc5_ < _loc4_.length)
      {
         _loc6_ = Number(_loc4_[_loc5_]);
         if(!_global.isNaN(_loc6_))
         {
            this._aTemplateIds.push(_loc6_);
         }
         _loc7_ = this.api.lang.getMonstersText(_loc4_[_loc5_]);
         this._aNamesList.push(_loc7_.n);
         if(_loc7_.a != -1)
         {
            this._nAlignmentIndex = _loc7_.a;
         }
         _loc5_ += 1;
      }
   }
   function get name()
   {
      return this.getName();
   }
   function get templateIds()
   {
      return this._aTemplateIds;
   }
   function get rawTemplateIds()
   {
      return this._sRawTemplateIds;
   }
   function getName(sEndChar)
   {
      sEndChar = sEndChar != undefined ? sEndChar : "\n";
      var _loc3_ = [];
      var _loc4_ = 0;
      while(_loc4_ < this._aLevelsList.length)
      {
         _loc3_.push({level:Number(this._aLevelsList[_loc4_]),name:this._aNamesList[_loc4_]});
         _loc4_ += 1;
      }
      _loc3_.sortOn(["level"],Array.DESCENDING | Array.NUMERIC);
      var _loc5_ = new String();
      var _loc6_ = 0;
      var _loc7_;
      while(_loc6_ < _loc3_.length)
      {
         _loc7_ = _loc3_[_loc6_];
         _loc5_ += _loc7_.name + " (" + _loc7_.level + ")" + sEndChar;
         _loc6_ += 1;
      }
      return _loc5_;
   }
   function alertChatText()
   {
      var _loc2_ = this.api.datacenter.Map;
      return "Groupe niveau " + this.totalLevel + " en " + _loc2_.x + "," + _loc2_.y + " : <br/>" + this.getName("<br/>");
   }
   function set Level(value)
   {
      this._aLevelsList = value.split(",");
   }
   function get totalLevel()
   {
      var _loc2_ = 0;
      var _loc3_ = 0;
      while(_loc3_ < this._aLevelsList.length)
      {
         _loc2_ += Number(this._aLevelsList[_loc3_]);
         _loc3_ += 1;
      }
      return _loc2_;
   }
   function get bonusValue()
   {
      return this._nBonusValue;
   }
   function get alignment()
   {
      return new dofus.datacenter.Alignment(this._nAlignmentIndex,0);
   }
}
