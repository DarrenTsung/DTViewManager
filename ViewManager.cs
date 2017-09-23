using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using DTObjectPoolManager;
using DTViewManager.Internal;

namespace DTViewManager {
	public class ViewManager : MonoBehaviour {
		// PRAGMA MARK - Public Interface
		public void AttachView(GameObject view) {
			if (view.transform.parent != this.transform) {
				view.transform.SetParent(this.transform, worldPositionStays: false);
			}

			// if the application is not playing, we don't need to manage the view order
			if (!Application.isPlaying) {
				return;
			}

			int priority;
			// NOTE (darren): Normally the view.transform will not be cached at this
			// point, but it can happen in edge cases (like CreateView() calling CreateView() in recycle setup)
			if (!cachedPriorities_.ContainsKey(view.transform)) {
				priority = GetPriority(view);
				cachedPriorities_[view.transform] = priority;
			} else {
				priority = cachedPriorities_[view.transform];
			}

			for (int i = 0; i < this.transform.childCount; i++) {
				Transform child = this.transform.GetChild(i);

				if (!cachedPriorities_.ContainsKey(child)) {
					cachedPriorities_[child] = GetPriority(child.gameObject);
					continue;
				}

				int childPriority = cachedPriorities_[child];
				if (childPriority > priority) {
					view.transform.SetSiblingIndex(i);
					break;
				}
			}
		}

		public Canvas Canvas {
			get { return canvas_; }
		}

		public CanvasScaler CanvasScaler {
			get { return canvasScaler_; }
		}


		// PRAGMA MARK - Internal
		[SerializeField]
		private List<ViewPriorityPair> serializedPriorities_;
		[SerializeField]
		private int defaultPriority_ = 100;

		private ViewPriorityMap priorityMap_;
		private Dictionary<Transform, int> cachedPriorities_ = new Dictionary<Transform, int>();

		private Canvas canvas_;
		private CanvasScaler canvasScaler_;

		private void Awake() {
			canvas_ = GetComponent<Canvas>();
			canvasScaler_ = GetComponent<CanvasScaler>();

			priorityMap_ = new ViewPriorityMap(defaultPriority_);
			foreach (var viewPriorityPair in serializedPriorities_) {
				priorityMap_.SetPriorityForPrefabName(viewPriorityPair.PrefabName, viewPriorityPair.Priority);
			}

			foreach (Transform child in this.transform) {
				cachedPriorities_[child] = defaultPriority_;
			}
		}

		private int GetPriority(GameObject view) {
			RecyclablePrefab r = view.GetRequiredComponent<RecyclablePrefab>();
			if (r == null) {
				Debug.LogWarning("Assigning default priority for gameObject: " + view + " because no RecycablePrefab exists!");
				return defaultPriority_;
			}

			return priorityMap_.PriorityForPrefabName(r.PrefabName);
		}
	}

	[Serializable]
	public class ViewPriorityPair {
		public string PrefabName;
		public int Priority;
	}
}