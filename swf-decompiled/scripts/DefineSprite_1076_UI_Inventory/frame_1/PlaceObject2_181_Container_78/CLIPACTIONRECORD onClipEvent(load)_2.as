onClipEvent(load){
   function __f_drop(eventObj)
   {
      _parent.dropItem({target:this});
   }
   this.addEventListener("drop",__f_drop);
}
