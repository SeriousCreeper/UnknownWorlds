using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

public class NoiseGenerator : EditorWindow
{
	public int resolution = 512;
	public int octaves = 8;
	public bool seamless;
	public float frequency = 2.0f;
	public float amplitude = 1.0f;
	public bool useSeed;
	public int seed;
	public Texture2D image;

	private float[,] _map, _r, _g, _b, _a;
	private float _min, _max;

	[MenuItem("Noise/Noise generator")]
	public static void ShowWindow()
	{
		GetWindow(typeof(NoiseGenerator));
	}

	public void OnDisable()
	{
		if (image != null)
		{
			DestroyImmediate(image);
		}
	}

	public void OnGUI()
	{
		resolution = EditorGUILayout.IntField("Resolution", resolution);
		octaves = EditorGUILayout.IntSlider("Octaves", octaves, 1, 64);
		frequency = EditorGUILayout.FloatField("Frequency", frequency);
		amplitude = EditorGUILayout.FloatField("Amplitude", amplitude);

		GUILayout.BeginHorizontal();
		{
			seamless = EditorGUILayout.Toggle("Seamless", seamless, GUILayout.ExpandWidth(false));
			useSeed = EditorGUILayout.Toggle("Use seed", useSeed, GUILayout.ExpandWidth(false));
			seed = EditorGUILayout.IntField("Seed", seed, GUILayout.ExpandWidth(false));
		}
		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();
		{
			if (GUILayout.Button("sum(noise)", GUILayout.ExpandWidth(false)))
			{
				GenerateNoise();

				UpdateImage();
			}

			if (GUILayout.Button("sum(|noise|)", GUILayout.ExpandWidth(false)))
			{
				GenerateTurbulentNoise();

				UpdateImage();
			}

			if (GUILayout.Button("sin(x + sum(|noise|))", GUILayout.ExpandWidth(false)))
			{
				GenerateSinusNoise();

				UpdateImage();
			}

			GUILayout.Label(string.Format("Min={0} Max={1}", _min, _max));
		}
		GUILayout.EndHorizontal();

		GUILayout.Space(10);

		if (image != null)
		{
			GUILayout.BeginHorizontal();
			{

				if (GUILayout.Button("Normalize(0,1)"))
				{
					NormalizeMap(_map,1f);
					UpdateImage();
				}

				if (GUILayout.Button("Clamp(0,1)"))
				{
					ApplyFunctionToMap(_map, Mathf.Clamp01);
					UpdateImage();
				}

				if (GUILayout.Button("x*x"))
				{
					ApplyFunctionToMap(_map, x => x * x);
					UpdateImage();
				}

				if (GUILayout.Button("sqrt(x)"))
				{
					ApplyFunctionToMap(_map, x => (float)Math.Sqrt(x));
					UpdateImage();
				}

				if (GUILayout.Button("1-x"))
				{
					ApplyFunctionToMap(_map, x => 1 - x);
					UpdateImage();
				}

				if (GUILayout.Button("Sharpen"))
				{
					ApplyFunctionToMap(_map, x =>
												{
													const float CLOUD_COVER = 0.1f;
													const float CLOUD_SHARPNESS = 0.5f;

													float c = x - CLOUD_COVER;
													if (c < 0)
													{
														c = 0;
													}

													float cloudDensity = 1 - (float)Math.Pow(CLOUD_SHARPNESS, c);

													return cloudDensity;
												});
					NormalizeMap(_map,1f);
					UpdateImage();
				}
			}
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			{
				GUILayout.Label("Store noise to channel ", GUILayout.Width(170));
				if (GUILayout.Button("R", GUILayout.ExpandWidth(false)))
				{
					if (_r == null)
					{
						_r = new float[resolution, resolution];
					}
				}
				if (GUILayout.Button("G", GUILayout.ExpandWidth(false)))
				{
					if (_g == null)
					{
						_g = new float[resolution, resolution];
					}
				}
				if (GUILayout.Button("B", GUILayout.ExpandWidth(false)))
				{
					if (_b == null)
					{
						_b = new float[resolution, resolution];
					}
				}
				if (GUILayout.Button("A", GUILayout.ExpandWidth(false)))
				{
					if (_a == null)
					{
						_a = new float[resolution, resolution];
					}
				}
				if (GUILayout.Button("RGB", GUILayout.ExpandWidth(false)))
				{
					SaveToFile();
				}
				if (GUILayout.Button("Save as .PNG", GUILayout.ExpandWidth(false)))
				{
					SaveToFile();
				}
			}
			GUILayout.EndHorizontal();
			GUILayout.BeginHorizontal();
			{
				GUILayout.Label("Restore noise from channel ", GUILayout.Width(170));
				if (GUILayout.Button("R", GUILayout.ExpandWidth(false)))
				{
					SaveToFile();
				}
				if (GUILayout.Button("G", GUILayout.ExpandWidth(false)))
				{
					SaveToFile();
				}
				if (GUILayout.Button("B", GUILayout.ExpandWidth(false)))
				{
					SaveToFile();
				}
				if (GUILayout.Button("A", GUILayout.ExpandWidth(false)))
				{
					SaveToFile();
				}
				if (GUILayout.Button("RGB", GUILayout.ExpandWidth(false)))
				{
					SaveToFile();
				}
			}
			GUILayout.EndHorizontal();

			GUILayout.Space(10);
		}

		if (seamless)
		{
			for (int i = 0; i < 4; i++)
			{
				GUILayout.BeginHorizontal();
				{
					for (int j = 0; j < 4; j++)
					{
						GUILayout.Label(image, GUILayout.ExpandWidth(false));
					}
				}
				GUILayout.EndHorizontal();
			}
		}
		else
		{
			GUILayout.Label(image);
		}
	}

	private void UpdateImage()
	{
		if (image != null)
		{
			DestroyImmediate(image);
		}
		image = new Texture2D(resolution, resolution, TextureFormat.RGB24, false);

		for (int x = 0; x < resolution; x++)
		{
			for (int y = 0; y < resolution; y++)
			{
				image.SetPixel(x, resolution - 1 - y, Color.white * _map[x, y]);
			}
		}

		image.Apply();

		FindMinMax(_map, out _min, out _max);
	}

	private void SaveToFile()
	{
		string filename = EditorUtility.SaveFilePanel("Save noise as PNG", null, "noise.png", "png");
		if (string.IsNullOrEmpty(filename))
		{
			return;
		}

		var bytes = image.EncodeToPNG();
		File.WriteAllBytes(filename, bytes);
	}

	private void SetSeed()
	{
		seed = useSeed ? seed : Random.Range(0, int.MaxValue);
	}

	private void GenerateNoise()
	{
		SetSeed();

		NoiseBase noise = new SimplexNoise(seed);
		_map = noise.CreateSimpleMap(resolution, octaves, frequency, amplitude, seamless);
	}

	private void GenerateSinusNoise()
	{
		SetSeed();

		var noise = new SimplexNoise(Random.Range(0, 1000));
		_map = noise.CreateSinusMap(resolution, octaves, frequency, amplitude, seamless);
	}

	private void GenerateTurbulentNoise()
	{
		SetSeed();

		var noise = new SimplexNoise(seed);
		_map = noise.CreateTurbilenceMap(resolution, octaves, frequency, amplitude, seamless);
	}

	public void NormalizeMap(float[,] map, float range)
	{
		int size = map.GetLength(0);

		float min, max;
		FindMinMax(map, out min, out max);

		float k = range / (max - min);
		for (int z = 0; z < size; z++)
		{
			for (int x = 0; x < size; x++)
			{
				map[z, x] = (map[z, x] - min) * k;
			}
		}
	}

	public void ApplyFunctionToMap(float[,] map, Func<float, float> function)
	{
		int size = map.GetLength(0);

		for (int z = 0; z < size; z++)
		{
			for (int x = 0; x < size; x++)
			{
				map[z, x] = function(map[z, x]);
			}
		}
	}

	public void FindMinMax(float[,] map, out float min, out float max)
	{
		int size = map.GetLength(0);

		min = float.MaxValue;
		max = float.MinValue;

		for (int z = 0; z < size; z++)
		{
			for (int x = 0; x < size; x++)
			{
				float value = map[z, x];

				if (value < min)
				{
					min = value;
				}
				else if (value > max)
				{
					max = value;
				}
			}
		}
	}
}