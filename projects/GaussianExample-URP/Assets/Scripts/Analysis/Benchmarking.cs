using Analysis.Logging;
using Dependencies;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Timing;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Analysis
{
	public class Benchmarking : MonoBehaviour
	{
		[SerializeField]
		private GameObject _startSceneCamera;

		[SerializeField]
		private int[] _sceneIndices;

		private Queue<int> _scenesToPlay = new Queue<int>();

		//Benchmarking 
		private FPSCalculator _fpsCalculator = new FPSCalculator();
		private LoadingMonitor _loadingMonitor;

		private IPerformanceReporter _analysisLogger = null;
		private ITimeService _timeService = null;
		private void Awake()
		{
			_analysisLogger = DependencyService.GetService<IPerformanceReporter>();
			_timeService = DependencyService.GetService<ITimeService>();
			_loadingMonitor = new LoadingMonitor(_analysisLogger);
			DontDestroyOnLoad(this);

			foreach(int index in _sceneIndices)
			{
				_scenesToPlay.Enqueue(index);
			}
		}

		private void Start()
		{
			LoadNextScene(0f);
		}

		public void LoadScene(int sceneIndex)
		{
			var loadSceneOperation = SceneManager.LoadSceneAsync(sceneIndex, LoadSceneMode.Additive);
			_loadingMonitor.MonitorAsyncOperation(loadSceneOperation, LoadEvent.LoadEventType.LoadScene, $"Scene_{sceneIndex}");
			loadSceneOperation.completed += LoadSceneOperation_completed;
		}

		public void UnLoadScene(int sceneIndex)
		{
			var unloadSceneOperation = SceneManager.UnloadSceneAsync(sceneIndex);
			_loadingMonitor.MonitorAsyncOperation(unloadSceneOperation, LoadEvent.LoadEventType.UnloadScene, $"Scene_{sceneIndex}");
			_startSceneCamera.SetActive(true);

		}

		//private void UnloadSceneOperation_completed(AsyncOperation obj)
		//{
		//	obj.completed -= UnloadSceneOperation_completed;

		//}

		private void LoadSceneOperation_completed(AsyncOperation obj)
		{
			obj.completed -= LoadSceneOperation_completed;
			_startSceneCamera.SetActive(false);

			//start autoplay
			AutoPlaySystem autoPlay = FindAnyObjectByType<AutoPlaySystem>();
			if (autoPlay != null)
			{
				autoPlay.FinishedPlaying += AutoPlay_FinishedPlaying;
				autoPlay.ShouldAutoPlay = true;
			}
		}

		private void AutoPlay_FinishedPlaying(object sender, System.EventArgs e)
		{
			(sender as AutoPlaySystem).FinishedPlaying -= AutoPlay_FinishedPlaying;
			//unload scene
			UnLoadScene(_scenesToPlay.Dequeue());

			LoadNextScene(3f);
		}

		public void RegisterFPS()
		{
			float deltaTime = Time.deltaTime;
			_fpsCalculator.RegisterFrame(deltaTime);
			_analysisLogger.ReportFPS(deltaTime, _fpsCalculator.AverageFPS);
		}



		public void Update()
		{
			
			RegisterFPS();
		}

		private void LoadNextScene(float delay = 0f)
		{
			if (_scenesToPlay.Count > 0)
			{
				if (delay > 0f)
				{
					StartCoroutine(WaitThenLoadScene(_scenesToPlay.Peek(), delay));
				}
				else
				{
					LoadScene(_scenesToPlay.Peek());
				}
			}
			else
			{
				StartCoroutine(WaitThenStop(delay));
			}
		}

		IEnumerator WaitThenLoadScene(int sceneIndex, float delay)
		{
			if (delay > 0f)
			{
				yield return _timeService.WaitForSeconds(delay) ;
			}
			LoadScene(sceneIndex);
		}

		IEnumerator WaitThenStop(float delay)
		{
			yield return _timeService.WaitForSeconds(delay);
			_analysisLogger.ReportCompleted();
			this.enabled = false;

		}
	}
}