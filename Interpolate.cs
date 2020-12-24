using UnityEngine;
using System.Collections.Generic;

//Interpolation between points with a Catmull-Rom spline
public class CatmullRomSpline
{
	//Has to be at least 4 points
	public List<Vector3> controlPointsList = new List<Vector3> ();
	//Are we making a line or a loop?
	public bool LoopBack = false;

	//Display a spline between 2 points derived with the Catmull-Rom spline algorithm
	public Vector3 Interpolate(int pos, float t)
	{
		//The 4 points we need to form a spline between p1 and p2
		Vector3 p0 = controlPointsList[ClampListPos(pos - 1)];
		Vector3 p1 = controlPointsList[pos];
		Vector3 p2 = controlPointsList[ClampListPos(pos + 1)];
		Vector3 p3 = controlPointsList[ClampListPos(pos + 2)];

		Vector3 Pos = GetCatmullRomPosition(t, p0, p1, p2, p3);;
		return Pos;

	}

	//Clamp the list positions to allow looping
	int ClampListPos(int pos)
	{
		if (pos < 0)
		{
			pos = controlPointsList.Count - 1;
		}

		if (pos > controlPointsList.Count)
		{
			pos = 1;
		}
		else if (pos > controlPointsList.Count - 1)
		{
			pos = 0;
		}

		return pos;
	}

	//Returns a position between 4 Vector3 with Catmull-Rom spline algorithm
	//http://www.iquilezles.org/www/articles/minispline/minispline.htm
	Vector3 GetCatmullRomPosition(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
	{
		//The coefficients of the cubic polynomial (except the 0.5f * which I added later for performance)
		Vector3 a = 2f * p1;
		Vector3 b = p2 - p0;
		Vector3 c = 2f * p0 - 5f * p1 + 4f * p2 - p3;
		Vector3 d = -p0 + 3f * p1 - 3f * p2 + p3;

		//The cubic polynomial: a + b * t + c * t^2 + d * t^3
		Vector3 pos = 0.5f * (a + (b * t) + (c * t * t) + (d * t * t * t));

		return pos;
	}
}