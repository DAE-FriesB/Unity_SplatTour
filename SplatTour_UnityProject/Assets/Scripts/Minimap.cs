using GaussianSplatting.Runtime;
using UnityEngine;
using UnityEngine.UI;

public class Minimap : MonoBehaviour
{
	[SerializeField]
	private GameObject _mapCellPrefab;

	[SerializeField]
	private SplatSplitter _splitter;
	// Start is called once before the first execution of Update after the MonoBehaviour is created
	[SerializeField]
	private float _cellSize = 100f;

	private Image[] _images;

	[SerializeField]
	private RectTransform _fovIndicator;
	void Start()
	{
		Vector2 botLeftPos = new Vector2(-(_splitter.NumColumns - 1) * _cellSize, -(_splitter.NumRows - 1) * _cellSize);

		_images = new Image[_splitter.NumRows * _splitter.NumColumns];

		//Vector2 topLeftPos = topRightPos - Vector2.left * rectWidth * _splitter.NumColumns;
		for (int row = 0; row < _splitter.NumRows; ++row)
		{
			for (int col = 0; col < _splitter.NumColumns; ++col)
			{
				int partitionIndex = _splitter.GetPartitionIndex(row, col);

				_images[partitionIndex] = Instantiate(_mapCellPrefab, transform).GetComponent<Image>();
				_images[partitionIndex].rectTransform.anchoredPosition = botLeftPos + Vector2.right * _cellSize * col + Vector2.up * _cellSize * row;
				_images[partitionIndex].rectTransform.sizeDelta = Vector2.one * _cellSize;
			}
		}
		_fovIndicator.SetAsLastSibling();
	}

	// Update is called once per frame
	void Update()
	{
		for (int partitionIndex = 0; partitionIndex < _images.Length; ++partitionIndex)
		{
			bool visible = _splitter.IsVisibleInCamera(partitionIndex);
			bool loaded = _splitter.GetPartition(partitionIndex).HasValidAsset;

			if (visible && loaded)
			{
				_images[partitionIndex].color = Color.green;
			}
			else if (loaded)
			{
				_images[partitionIndex].color = Color.white;
			}
			else
			{
				_images[partitionIndex].color = Color.gray;
			}
		}

		UpdateFOVIndicatorPosition();
		UpdateFOVIndicatorRotation();
		UpdateFOVIndicatorScale();


	}
	void UpdateFOVIndicatorScale()
	{
		float horizontalFOV = CalculateHorizontalFOV(Camera.main.fieldOfView, (float)Screen.width / Screen.height);
		float rectWidth = Mathf.Tan(horizontalFOV) * _fovIndicator.sizeDelta.y * 2;
		_fovIndicator.sizeDelta = new Vector2(rectWidth, _fovIndicator.sizeDelta.y);
	}
	void UpdateFOVIndicatorRotation()
	{
		_fovIndicator.rotation = Quaternion.Euler(0, 0, -Camera.main.transform.eulerAngles.y);
	}
	void UpdateFOVIndicatorPosition()
	{

		Vector3 relativePos = Camera.main.transform.position;
		//relativePos = _splitter.GetBounds(7).min;
		var bounds = _splitter.GetBounds(0);

		relativePos -= bounds.min;
		relativePos.x /= _splitter.PartitionSize.x;
		relativePos.z /= _splitter.PartitionSize.y;
		relativePos.y = 0f;

		Vector2 normalizedPos = new Vector2(relativePos.x, relativePos.z);
		//normalizedPos = new Vector2(1, 1);

		Vector2 botLeftPos = new Vector2(-_cellSize * _splitter.NumColumns, -_cellSize * _splitter.NumRows);

		_fovIndicator.anchoredPosition = botLeftPos + normalizedPos * _cellSize;

	}

	private float CalculateHorizontalFOV(float verticalFOV, float aspectRatio)
	{
		// Convert vertical FOV from degrees to radians
		float verticalFOVRadians = Mathf.Deg2Rad * verticalFOV;

		// Calculate the horizontal FOV in radians
		float horizontalFOVRadians = Mathf.Atan(Mathf.Tan(verticalFOVRadians / 2.0f) * aspectRatio);

		// Convert horizontal FOV back to degrees
		return horizontalFOVRadians;
	}
}
