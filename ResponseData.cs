using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace ConsoleMod
{

	// Token: 0x02000644 RID: 1604
	public partial class ResponseData
	{
		// Token: 0x0600335C RID: 13148 RVA: 0x000021DD File Offset: 0x000003DD
		public virtual void Populate(JSONObject jSONObject)
		{
		}

		// Token: 0x0600335D RID: 13149 RVA: 0x000BC904 File Offset: 0x000BAB04
		private string ParsePathSafe(JSONObject root, string id, string altId)
		{
			if (this.IsNull(root))
			{
				return string.Empty;
			}
			JSONObject jsonobject = this.FindObjectWithKey(root, id);
			if (jsonobject == null && !string.IsNullOrEmpty(altId))
			{
				jsonobject = this.FindObjectWithKey(root, altId);
			}
			if (this.IsNull(jsonobject))
			{
				return string.Empty;
			}
			if (string.IsNullOrEmpty(jsonobject.str))
			{
				return string.Empty;
			}
			return jsonobject.str.Replace("\\/", "/");
		}

		// Token: 0x0600335E RID: 13150 RVA: 0x0001E997 File Offset: 0x0001CB97
		private string ParsePathSafe(JSONObject root, string id)
		{
			return this.ParsePathSafe(root, id, null);
		}

		// Token: 0x0600335F RID: 13151 RVA: 0x000BC974 File Offset: 0x000BAB74
		private string ParseStringSafe(JSONObject root, string id, string altId)
		{
			if (this.IsNull(root))
			{
				return string.Empty;
			}
			JSONObject jsonobject = this.FindObjectWithKey(root, id);
			if (jsonobject == null && !string.IsNullOrEmpty(altId))
			{
				jsonobject = this.FindObjectWithKey(root, altId);
			}
			if (this.IsNull(jsonobject))
			{
				return string.Empty;
			}
			if (!jsonobject.IsString)
			{
				return jsonobject.n.ToString();
			}
			return jsonobject.str;
		}

		// Token: 0x06003360 RID: 13152 RVA: 0x0001E9A2 File Offset: 0x0001CBA2
		private string ParseStringSafe(JSONObject root, string id)
		{
			return this.ParseStringSafe(root, id, null);
		}

		// Token: 0x06003361 RID: 13153 RVA: 0x000BC9D8 File Offset: 0x000BABD8
		private int ParseIntSafe(JSONObject root, string id, string altId)
		{
			if (this.IsNull(root))
			{
				return 0;
			}
			JSONObject jsonobject = this.FindObjectWithKey(root, id);
			if (jsonobject == null && !string.IsNullOrEmpty(altId))
			{
				jsonobject = this.FindObjectWithKey(root, altId);
			}
			if (this.IsNull(jsonobject))
			{
				return 0;
			}
			return (int)jsonobject.n;
		}

		// Token: 0x06003362 RID: 13154 RVA: 0x0001E9AD File Offset: 0x0001CBAD
		private int ParseIntSafe(JSONObject root, string id)
		{
			return this.ParseIntSafe(root, id, null);
		}

		// Token: 0x06003363 RID: 13155 RVA: 0x000BCA20 File Offset: 0x000BAC20
		private float ParseFloatSafe(JSONObject root, string id, string altId)
		{
			if (this.IsNull(root))
			{
				return 0f;
			}
			JSONObject jsonobject = this.FindObjectWithKey(root, id);
			if (jsonobject == null && !string.IsNullOrEmpty(altId))
			{
				jsonobject = this.FindObjectWithKey(root, altId);
			}
			if (this.IsNull(jsonobject))
			{
				return 0f;
			}
			return jsonobject.n;
		}

		// Token: 0x06003364 RID: 13156 RVA: 0x0001E9B8 File Offset: 0x0001CBB8
		private float ParseFloatSafe(JSONObject root, string id)
		{
			return this.ParseFloatSafe(root, id, null);
		}

		// Token: 0x06003365 RID: 13157 RVA: 0x000BCA70 File Offset: 0x000BAC70
		private bool ParseBoolSafe(JSONObject root, string id, string altId)
		{
			if (this.IsNull(root))
			{
				return false;
			}
			JSONObject jsonobject = this.FindObjectWithKey(root, id);
			if (jsonobject == null && !string.IsNullOrEmpty(altId))
			{
				jsonobject = this.FindObjectWithKey(root, altId);
			}
			return !this.IsNull(jsonobject) && jsonobject.b;
		}

		// Token: 0x06003366 RID: 13158 RVA: 0x0001E9C3 File Offset: 0x0001CBC3
		private bool ParseBoolSafe(JSONObject root, string id)
		{
			return this.ParseBoolSafe(root, id, null);
		}

		// Token: 0x06003367 RID: 13159 RVA: 0x0001E9CE File Offset: 0x0001CBCE
		private bool IsNull(JSONObject jsonObject)
		{
			return jsonObject == null || jsonObject.IsNull;
		}

		// Token: 0x06003368 RID: 13160 RVA: 0x000BCAB8 File Offset: 0x000BACB8
		private JSONObject FindObjectWithKey(JSONObject jSONObject, string key)
		{
			if (this.IsNull(jSONObject) || jSONObject.keys == null)
			{
				return null;
			}
			for (int i = 0; i < jSONObject.keys.Count; i++)
			{
				if (jSONObject.keys[i] == key)
				{
					return jSONObject.list[i];
				}
			}
			return null;
		}

		// Token: 0x06003369 RID: 13161 RVA: 0x000BCB10 File Offset: 0x000BAD10
		private string ExtractString(JSONObject jSONObject, string key)
		{
			if (this.IsNull(jSONObject) || jSONObject.keys == null)
			{
				return null;
			}
			for (int i = 0; i < jSONObject.keys.Count; i++)
			{
				if (jSONObject.keys[i] == key && jSONObject.list[i].IsString)
				{
					return jSONObject.list[i].str;
				}
			}
			return "";
		}
		public class User : ResponseData
		{
			public override void Populate(JSONObject jSONObject)
			{
				this.id = base.ParseStringSafe(jSONObject, "id");
				this.displayName = base.ParseStringSafe(jSONObject, "display_name");
                this.platform = base.ParseStringSafe(jSONObject, "platform");
                this.followers = base.ParseIntSafe(jSONObject, "followers");
                this.isBanned = base.ParseBoolSafe(jSONObject, "is_banned");
            }

			public User()
			{
			}
			public string id;
			public string displayName;
            public string platform;
            public int followers;
            public bool isBanned;
		}
	}
}
