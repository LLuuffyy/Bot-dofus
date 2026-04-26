onClipEvent(load){
   function __f_drag(eventObj)
   {
      _parent.dragItem({target:this});
   }
   this.addEventListener("drag",__f_drag);
}
