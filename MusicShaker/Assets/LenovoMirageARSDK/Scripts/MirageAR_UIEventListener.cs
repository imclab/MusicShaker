﻿namespace LenovoMirageARSDK
{
	using UnityEngine;
	using UnityEngine.EventSystems;


	public class MirageAR_UIEventListener : EventTrigger
	{
		public System.Action onClick;
        public System.Action<BaseEventData> onClickWithData;

        public System.Action<GameObject> onSelect;
		public System.Action<GameObject> onUpdateSelect;

		public System.Action<BaseEventData> onPointerDown;
		public System.Action<BaseEventData> onPointerEnter;
		public System.Action<BaseEventData> onPointerExit;
		public System.Action<BaseEventData> onPointerUp;

		public System.Action<BaseEventData> onBeginDrag;
		public System.Action<BaseEventData> onEndDrag;
		public System.Action<BaseEventData> onDrag;

		public System.Action<bool> onValueChanged;

		public static MirageAR_UIEventListener CheckAndAddListener(GameObject go)
		{
            MirageAR_UIEventListener listener = go.GetComponent<MirageAR_UIEventListener>();
			if (listener == null) listener = go.AddComponent<MirageAR_UIEventListener>();

			return listener;
		}
		public static MirageAR_UIEventListener Get(GameObject go)
		{
			return CheckAndAddListener (go);
		}

		public override void OnPointerClick(PointerEventData eventData)
		{
			if (onClick != null) onClick();

            if (onClickWithData != null) onClickWithData(eventData);            
		}
		public override void OnPointerDown(PointerEventData eventData)
		{
			if (onPointerDown != null) onPointerDown(eventData);
		}
		public override void OnPointerEnter(PointerEventData eventData)
		{
			if (onPointerEnter != null) onPointerEnter(eventData);
		}
		public override void OnPointerExit(PointerEventData eventData)
		{
			if (onPointerExit != null) onPointerExit(eventData);
		}
		public override void OnPointerUp(PointerEventData eventData)
		{
			if (onPointerUp != null) onPointerUp(eventData);
		}
		public override void OnSelect(BaseEventData eventData)
		{
			if (onSelect != null) onSelect(gameObject);
		}
		public override void OnUpdateSelected(BaseEventData eventData)
		{
			if (onUpdateSelect != null) onUpdateSelect(gameObject);
		}
		public override void OnBeginDrag(PointerEventData eventData)
		{
			if (onBeginDrag != null) onBeginDrag(eventData);
		}
		public override void OnEndDrag(PointerEventData eventData)
		{
			if (onEndDrag != null) onEndDrag(eventData);
		}
		public override void OnDrag(PointerEventData eventData) 
		{
			if (onDrag != null) onDrag(eventData);
		}

	    void OnDestroy()
	    {
	        onClick = null;
	        onSelect = null;
	        onUpdateSelect = null;

	        onPointerDown = null;
	        onPointerEnter = null;
	        onPointerExit = null;
	        onPointerUp = null;

	        onBeginDrag = null;
	        onEndDrag = null;
	        onDrag = null;

	        onValueChanged = null;
	    }
	}
}