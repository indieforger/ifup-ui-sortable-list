﻿using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace ifup.ui
{

    // todo: 
    // - override draggingDelay on each list

    public class SortableListManager : Singleton<SortableListManager>
    {
        public float draggingDelay = 0.25f;

        private List<SortableList> m_sortableLists = new List<SortableList>();
        private SortableListItem m_draggedItem;
        private SortableListItem m_mockItem;

        private SortableList m_sourceList;
        private int m_sourceItemIndex = -1;

        private SortableList m_targetList;
        private int m_targetItemIndex = -1;

        private bool m_globalScrollLock = false;
        private bool m_dragPrepping = false;
        private bool m_dragActivated = false;
        private float m_pressTime0 = 0;
        private Vector3 m_pressPosition0 = Vector3.zero;

        protected void Start()
        {
            GameObject go = new GameObject("Mock List Item");
            go.AddComponent<RectTransform>();
            go.AddComponent<CanvasRenderer>();
            go.AddComponent<LayoutElement>();
            m_mockItem = go.AddComponent<SortableListItem>();
            m_mockItem.gameObject.SetActive(false);
        }

        protected void Update()
        {
            if (m_dragPrepping || m_dragActivated) {

                UpdateActiveList();

                if (m_dragPrepping && Time.time - m_pressTime0 > draggingDelay && m_pressPosition0 == Input.mousePosition) {
                    StartDragging();
                }

                if (m_dragActivated) {
                    if (Input.GetMouseButtonUp(0)) {
                        StopDragging();
                        AttachDraggedItem();
                    } else {
                        UpdateDraggingPosition();
                    }
                }
            }
        }

        public void Register(SortableList sortableList)
        {
            if (!m_sortableLists.Contains(sortableList)) {
                m_sortableLists.Add(sortableList);
                sortableList.OnListItemPressed += OnListItemPressed;
            }
        }

        public void Unregister(SortableList sortableList)
        {
            if (m_sortableLists.Contains(sortableList)) {
                m_sortableLists.Remove(sortableList);
                sortableList.OnListItemReleased += OnListItemReleased;
            }
        }

        public bool scrollLock
        {
            get
            {
                return m_globalScrollLock;
            }
        }

        private void OnListItemPressed(SortableList sortableList, SortableListItem sortableListItem)
        {
            m_sourceList = sortableList;
            m_draggedItem = sortableListItem;
            m_sourceItemIndex = m_draggedItem.transform.GetSiblingIndex();
            m_dragPrepping = true;
            m_pressTime0 = Time.time;
            m_pressPosition0 = Input.mousePosition;
        }

        private void OnListItemReleased(SortableList SortableList, SortableListItem sortableListItem)
        {
            m_sourceList = null;
            m_draggedItem = null;
            m_dragPrepping = false;
            m_pressTime0 = 0;
            m_pressPosition0 = Vector3.zero;
        }

        private void UpdateDraggingPosition()
        {
            if (m_draggedItem == null) return;
            m_draggedItem.transform.position = Input.mousePosition;
        }

        private void UpdateActiveList()
        {
            Vector3 mousePos = Input.mousePosition;
            foreach (SortableList sortableList in m_sortableLists) {
                if (IsMouseOveRectTransform(sortableList.transform as RectTransform)) {
                    m_targetList = sortableList;
                    break;
                }
            }

            if (m_targetList == null) return;
            if (m_dragActivated == false) return;

            m_mockItem.gameObject.SetActive(true);
            if (m_mockItem == null || m_mockItem.layoutElement == null) return;

            int prevIndex = m_targetItemIndex;
            int itemIndex = -1;
            SortableListItem item = null;

            foreach (SortableListItem listItem in m_targetList.GetItems()) {
                itemIndex = listItem.transform.GetSiblingIndex();
                // TODO [1]: calculate manualy including padding to prevent flickering
                if (IsMouseOveRectTransform(listItem.transform as RectTransform)) {
                    m_targetItemIndex = itemIndex;
                    item = listItem;
                    break;
                }
            }

            // Flickering fix: 
            // TODO [2]: can be removed as soon as above todo [1] is completed
            if (item != null) {
                m_mockItem.layoutElement.minWidth = item.layoutElement.minWidth;
                m_mockItem.layoutElement.minHeight = item.layoutElement.minHeight;
                m_mockItem.layoutElement.preferredWidth = item.layoutElement.preferredWidth;
                m_mockItem.layoutElement.preferredHeight = item.layoutElement.preferredHeight;
            } else {
                m_mockItem.layoutElement.minWidth = m_draggedItem.layoutElement.minWidth;
                m_mockItem.layoutElement.minHeight = m_draggedItem.layoutElement.minHeight;
                m_mockItem.layoutElement.preferredWidth = m_draggedItem.layoutElement.preferredWidth;
                m_mockItem.layoutElement.preferredHeight = m_draggedItem.layoutElement.preferredHeight;
            }

            if (m_targetItemIndex != itemIndex) {
                m_targetItemIndex = itemIndex + 1;
            }

            if (prevIndex == m_targetItemIndex) return;


            m_targetList.AttachItem(m_mockItem, m_targetItemIndex);
            m_sourceList.UpdateContentSize();

        }

        private void StartDragging()
        {
            m_dragActivated = true;
            m_dragPrepping = false;
            ToggleScrollLock(true);
            m_sourceList.DetachItem(m_draggedItem, m_sourceList.canvas.transform);
        }

        private void StopDragging()
        {
            m_dragActivated = false;
            ToggleScrollLock(false);
        }

        private void AttachDraggedItem()
        {
            int itemIndex = m_targetItemIndex;
            SortableList sortableList = m_targetList;

            if (m_targetList == null) {
                m_targetList = m_sourceList;
                itemIndex = m_sourceItemIndex;
            }

            sortableList.AttachItem(m_draggedItem, itemIndex);

            m_targetList.DetachItem(m_mockItem, m_targetList.canvas.transform);
            m_mockItem.gameObject.SetActive(false);

            m_draggedItem = null;
            m_targetList = null;
            m_targetItemIndex = -1;
            m_sourceList = null;
            m_sourceItemIndex = -1;
        }

        private bool IsMouseOveRectTransform(RectTransform rt)
        {
            Vector2 mousePosition = Input.mousePosition;
            Vector3[] worldCorners = new Vector3[4];
            rt.GetWorldCorners(worldCorners);

            if (mousePosition.x >= worldCorners[0].x && mousePosition.x < worldCorners[2].x
               && mousePosition.y >= worldCorners[0].y && mousePosition.y < worldCorners[2].y) {
                return true;
            }
            return false;
        }

        private void ToggleScrollLock(bool value)
        {
            foreach (SortableList sortableList in m_sortableLists) {
                sortableList.scrollLock = value;
            }
            m_globalScrollLock = value;
        }
    }
}