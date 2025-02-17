﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PubnubApi
{
    public class NewtonsoftJsonDotNet : IJsonPluggableLibrary
    {
        private readonly PNConfiguration config;
        private readonly IPubnubLog pubnubLog;
        #region "IL2CPP workarounds"
        //Got an exception when using JSON serialisation for [],
        //IL2CPP needs to know about the array type at compile time.
        //So please define private static filed like this:
#pragma warning disable
        private static readonly System.String[][] _unused;
        private static readonly System.Int32[][] _unused2;
        private static readonly System.Int64[][] _unused3;
        private static readonly System.Int16[][] _unused4;
        private static readonly System.UInt16[][] _unused5;
        private static readonly System.UInt64[][] _unused6;
        private static readonly System.UInt32[][] _unused7;
        private static readonly System.Decimal[][] _unused8;
        private static readonly System.Double[][] _unused9;
        private static readonly System.Boolean[][] _unused91;
        private static readonly System.Object[][] _unused92;

        private static readonly long[][] _unused10;
        private static readonly int[][] _unused11;
        private static readonly float[][] _unused12;
        private static readonly decimal[][] _unused13;
        private static readonly uint[][] _unused14;
        private static readonly ulong[][] _unused15;
#pragma warning restore

        #endregion

        public NewtonsoftJsonDotNet(PNConfiguration pubnubConfig, IPubnubLog log)
        {
            this.config = pubnubConfig;
            this.pubnubLog = log;
        }

        #region IJsonPlugableLibrary methods implementation
        private static bool IsValidJson(string jsonString, PNOperationType operationType)
        {
            bool ret = false;
            try
            {
                if (operationType == PNOperationType.PNPublishOperation 
                    || operationType == PNOperationType.PNHistoryOperation 
                    || operationType == PNOperationType.PNTimeOperation
                    || operationType == PNOperationType.PNPublishFileMessageOperation)
                {
                    JArray.Parse(jsonString);
                }
                else
                {
                    JObject.Parse(jsonString);
                }
                ret = true;
            }
            catch
            {
                try
                {
                    if (operationType == PNOperationType.PNPublishOperation
                        || operationType == PNOperationType.PNHistoryOperation
                        || operationType == PNOperationType.PNTimeOperation
                        || operationType == PNOperationType.PNPublishFileMessageOperation)
                    {
                        JObject.Parse(jsonString);
                        ret = true;
                    }
                }
                catch { /* igonore */ }
            }
            return ret;
        }

        public object BuildJsonObject(string jsonString)
        {
            object ret = null;

            try
            {
                var token = JToken.Parse(jsonString);
                ret = token;
            }
            catch {  /* ignore */ }

            return ret;
        }

        public bool IsDictionaryCompatible(string jsonString, PNOperationType operationType)
        {
            bool ret = false;
            if (IsValidJson(jsonString, operationType))
            {
                try
                {
                    using (StringReader strReader = new StringReader(jsonString))
                    {
                        using (JsonTextReader jsonTxtreader = new JsonTextReader(strReader))
                        {
                            while (jsonTxtreader.Read())
                            {
                                if (jsonTxtreader.LineNumber == 1 && jsonTxtreader.LinePosition == 1 && jsonTxtreader.TokenType == JsonToken.StartObject)
                                {
                                    ret = true;
                                    break;
                                }
                                else
                                {
                                    break;
                                }
                            }
                            jsonTxtreader.Close();
                        }
#if (NET35 || NET40 || NET45 || NET461)
                        strReader.Close();
#endif
                    }
                }
                catch {  /* ignore */ }
            }
            return ret;
        }

        public string SerializeToJsonString(object objectToSerialize)
        {
            return JsonConvert.SerializeObject(objectToSerialize);
        }

        public List<object> DeserializeToListOfObject(string jsonString)
        {
            List<object> result = JsonConvert.DeserializeObject<List<object>>(jsonString);

            return result;
        }

        public object DeserializeToObject(string jsonString)
        {
            object result = JsonConvert.DeserializeObject<object>(jsonString, new JsonSerializerSettings { DateParseHandling = DateParseHandling.None });
            if (result.GetType().ToString() == "Newtonsoft.Json.Linq.JArray")
            {
                JArray jarrayResult = result as JArray;
                List<object> objectContainer = jarrayResult.ToObject<List<object>>();
                if (objectContainer != null && objectContainer.Count > 0)
                {
                    for (int index = 0; index < objectContainer.Count; index++)
                    {
                        if (objectContainer[index].GetType().ToString() == "Newtonsoft.Json.Linq.JArray")
                        {
                            JArray internalItem = objectContainer[index] as JArray;
                            objectContainer[index] = internalItem.Select(item => (object)item).ToArray();
                        }
                    }
                    result = objectContainer;
                }
            }
            return result;
        }

        public void PopulateObject(string value, object target)
        {
            JsonConvert.PopulateObject(value, target);
        }

        public virtual T DeserializeToObject<T>(string jsonString)
        {
            T ret = default(T);

            try
            {
                ret = JsonConvert.DeserializeObject<T>(jsonString, new JsonSerializerSettings { DateParseHandling = DateParseHandling.None });
            }
            catch { /* ignore */ }

            return ret;
        }

        private bool IsGenericTypeForMessage<T>()
        {
            bool ret = false;
            PNPlatform.Print(config, pubnubLog);

#if (NET35 || NET40 || NET45 || NET461)
            if (typeof(T).IsGenericType && typeof(T).GetGenericTypeDefinition() == typeof(PNMessageResult<>))
            {
                ret = true;
            }
            LoggingMethod.WriteToLog(pubnubLog, string.Format("DateTime: {0}, NET35/40 IsGenericTypeForMessage = {1}", DateTime.Now.ToString(CultureInfo.InvariantCulture), ret.ToString()), config.LogVerbosity);
#elif (NETSTANDARD10 || NETSTANDARD11 || NETSTANDARD12 || NETSTANDARD13 || NETSTANDARD14 || NETSTANDARD20 || UAP || NETFX_CORE || WINDOWS_UWP)
            if (typeof(T).GetTypeInfo().IsGenericType && typeof(T).GetGenericTypeDefinition() == typeof(PNMessageResult<>))
            {
                ret = true;
            }
            LoggingMethod.WriteToLog(pubnubLog, string.Format("DateTime: {0}, typeof(T).GetTypeInfo().IsGenericType = {1}", DateTime.Now.ToString(CultureInfo.InvariantCulture), typeof(T).GetTypeInfo().IsGenericType.ToString()), config.LogVerbosity);
            if (typeof(T).GetTypeInfo().IsGenericType)
            {
                LoggingMethod.WriteToLog(pubnubLog, string.Format("DateTime: {0}, typeof(T).GetGenericTypeDefinition() = {1}", DateTime.Now.ToString(CultureInfo.InvariantCulture), typeof(T).GetGenericTypeDefinition().ToString()), config.LogVerbosity);
            }
            LoggingMethod.WriteToLog(pubnubLog, string.Format("DateTime: {0}, PCL/CORE IsGenericTypeForMessage = {1}", DateTime.Now.ToString(CultureInfo.InvariantCulture), ret.ToString()), config.LogVerbosity);
#endif
            LoggingMethod.WriteToLog(pubnubLog, string.Format("DateTime: {0}, IsGenericTypeForMessage = {1}", DateTime.Now.ToString(CultureInfo.InvariantCulture), ret.ToString()), config.LogVerbosity);
            return ret;
        }

        private T DeserializeMessageToObjectBasedOnPlatform<T>(List<object> listObject)
        {
            T ret = default(T);

#if NET35 || NET40 || NET45 || NET461
            Type dataType = typeof(T).GetGenericArguments()[0];
            Type generic = typeof(PNMessageResult<>);
            Type specific = generic.MakeGenericType(dataType);

            ConstructorInfo ci = specific.GetConstructors().FirstOrDefault();
            if (ci != null)
            {
                object message = ci.Invoke(new object[] { });

                //Set data
                PropertyInfo dataProp = specific.GetProperty("Message");

                object userMessage = null;
                if (listObject[0].GetType() == typeof(Newtonsoft.Json.Linq.JValue))
                {
                    JValue jsonValue = listObject[0] as JValue;
                    userMessage = jsonValue.Value;
                    userMessage = ConvertToDataType(dataType, userMessage);

                    dataProp.SetValue(message, userMessage, null);
                }
                else if (listObject[0].GetType() == typeof(Newtonsoft.Json.Linq.JObject) || listObject[0].GetType() == typeof(Newtonsoft.Json.Linq.JArray))
                {
                    JToken token = listObject[0] as JToken;
                    if (dataProp.PropertyType == typeof(string))
                    {
                        userMessage = JsonConvert.SerializeObject(token);
                    }
                    else
                    {
                        userMessage = token.ToObject(dataProp.PropertyType, JsonSerializer.Create());
                    }

                    dataProp.SetValue(message, userMessage, null);
                }
                else if (listObject[0].GetType() == typeof(System.String))
                {
                    userMessage = listObject[0] as string;
                    dataProp.SetValue(message, userMessage, null);
                }

                //Set Time
                PropertyInfo timeProp = specific.GetProperty("Timetoken");
                long timetoken;
                Int64.TryParse(listObject[2].ToString(), out timetoken);
                timeProp.SetValue(message, timetoken, null);

                //Set Publisher
                PropertyInfo publisherProp = specific.GetProperty("Publisher");
                string publisherValue = (listObject[3] != null) ? listObject[3].ToString() : "";
                publisherProp.SetValue(message, publisherValue, null);

                // Set ChannelName
                PropertyInfo channelNameProp = specific.GetProperty("Channel");
                channelNameProp.SetValue(message, (listObject.Count == 6) ? listObject[5].ToString() : listObject[4].ToString(), null);

                // Set ChannelGroup
                if (listObject.Count == 6)
                {
                    PropertyInfo subsciptionProp = specific.GetProperty("Subscription");
                    subsciptionProp.SetValue(message, listObject[4].ToString(), null);
                }
                
                //Set Metadata list second position, index=1
                if (listObject[1] != null)
                {
                    PropertyInfo userMetadataProp = specific.GetProperty("UserMetadata");
                    userMetadataProp.SetValue(message, listObject[1], null);
                }

                ret = (T)Convert.ChangeType(message, specific, CultureInfo.InvariantCulture);
            }
#elif NETSTANDARD10 || NETSTANDARD11 || NETSTANDARD12 || NETSTANDARD13 || NETSTANDARD14 || NETSTANDARD20 || UAP || NETFX_CORE || WINDOWS_UWP
            Type dataType = typeof(T).GetTypeInfo().GenericTypeArguments[0];
            Type generic = typeof(PNMessageResult<>);
            Type specific = generic.MakeGenericType(dataType);

            ConstructorInfo ci = specific.GetTypeInfo().DeclaredConstructors.FirstOrDefault();
            if (ci != null)
            {
                object message = ci.Invoke(new object[] { });

                //Set data
                PropertyInfo dataProp = specific.GetRuntimeProperty("Message");

                object userMessage = null;
                if (listObject[0].GetType() == typeof(Newtonsoft.Json.Linq.JValue))
                {
                    JValue jsonValue = listObject[0] as JValue;
                    userMessage = jsonValue.Value;
                    userMessage = ConvertToDataType(dataType, userMessage);

                    dataProp.SetValue(message, userMessage, null);
                }
                else if (listObject[0].GetType() == typeof(Newtonsoft.Json.Linq.JObject) || listObject[0].GetType() == typeof(Newtonsoft.Json.Linq.JArray))
                {
                    JToken token = listObject[0] as JToken;
                    if (dataProp.PropertyType == typeof(string))
                    {
                        userMessage = JsonConvert.SerializeObject(token);
                    }
                    else
                    {
                        userMessage = token.ToObject(dataProp.PropertyType, JsonSerializer.Create());
                    }

                    dataProp.SetValue(message, userMessage, null);
                }
                else if (listObject[0].GetType() == typeof(System.String))
                {
                    userMessage = listObject[0] as string;
                    dataProp.SetValue(message, userMessage, null);
                }

                //Set Time
                PropertyInfo timeProp = specific.GetRuntimeProperty("Timetoken");
                long timetoken;
                Int64.TryParse(listObject[2].ToString(), out timetoken);
                timeProp.SetValue(message, timetoken, null);

                //Set Publisher
                PropertyInfo publisherProp = specific.GetRuntimeProperty("Publisher");
                string publisherValue = (listObject[3] != null) ? listObject[3].ToString() : "";
                publisherProp.SetValue(message, publisherValue, null);

                // Set ChannelName
                PropertyInfo channelNameProp = specific.GetRuntimeProperty("Channel");
                channelNameProp.SetValue(message, (listObject.Count == 6) ? listObject[5].ToString() : listObject[4].ToString(), null);

                // Set ChannelGroup
                if (listObject.Count == 6)
                {
                    PropertyInfo subsciptionProp = specific.GetRuntimeProperty("Subscription");
                    subsciptionProp.SetValue(message, listObject[4].ToString(), null);
                }

                //Set Metadata list second position, index=1
                if (listObject[1] != null)
                {
                    PropertyInfo userMetadataProp = specific.GetRuntimeProperty("UserMetadata");
                    userMetadataProp.SetValue(message, listObject[1], null);
                }

                ret = (T)Convert.ChangeType(message, specific, CultureInfo.InvariantCulture);
            }
#endif

            return ret;
        }

        public virtual T DeserializeToObject<T>(List<object> listObject)
        {
            T ret = default(T);

            if (listObject == null)
            {
                return ret;
            }

            if (IsGenericTypeForMessage<T>())
            {
#region "Subscribe Message<>"
                return DeserializeMessageToObjectBasedOnPlatform<T>(listObject);
#endregion
            }
            else if (typeof(T) == typeof(PNAccessManagerGrantResult))
            {
#region "PNAccessManagerGrantResult"
                PNAccessManagerGrantResult result = PNAccessManagerGrantJsonDataParse.GetObject(listObject);
                ret = (T)Convert.ChangeType(result, typeof(PNAccessManagerGrantResult), CultureInfo.InvariantCulture);
#endregion
            }
            else if (typeof(T) == typeof(PNAccessManagerTokenResult))
            {
                #region "PNAccessManagerTokenResult"
                PNAccessManagerTokenResult result = PNGrantTokenJsonDataParse.GetObject(listObject);
                ret = (T)Convert.ChangeType(result, typeof(PNAccessManagerTokenResult), CultureInfo.InvariantCulture);
                #endregion
            }
            else if (typeof(T) == typeof(PNAccessManagerRevokeTokenResult))
            {
                #region "PNAccessManagerRevokeTokenResult"
                PNAccessManagerRevokeTokenResult result = PNRevokeTokenJsonDataParse.GetObject(listObject);
                ret = (T)Convert.ChangeType(result, typeof(PNAccessManagerRevokeTokenResult), CultureInfo.InvariantCulture);
                #endregion
            }
            else if (typeof(T) == typeof(PNAccessManagerAuditResult))
            {
#region "PNAccessManagerAuditResult"
                Dictionary<string, object> auditDicObj = ConvertToDictionaryObject(listObject[0]);

                PNAccessManagerAuditResult ack = null;

                if (auditDicObj != null)
                {
                    ack = new PNAccessManagerAuditResult();

                    if (auditDicObj.ContainsKey("payload"))
                    {
                        Dictionary<string, object> auditAckPayloadDic = ConvertToDictionaryObject(auditDicObj["payload"]);
                        if (auditAckPayloadDic != null && auditAckPayloadDic.Count > 0)
                        {
                            if (auditAckPayloadDic.ContainsKey("level"))
                            {
                                ack.Level = auditAckPayloadDic["level"].ToString();
                            }

                            if (auditAckPayloadDic.ContainsKey("subscribe_key"))
                            {
                                ack.SubscribeKey = auditAckPayloadDic["subscribe_key"].ToString();
                            }

                            if (auditAckPayloadDic.ContainsKey("channel"))
                            {
                                ack.Channel = auditAckPayloadDic["channel"].ToString();
                            }

                            if (auditAckPayloadDic.ContainsKey("channel-group"))
                            {
                                ack.ChannelGroup = auditAckPayloadDic["channel-group"].ToString();
                            }

                            if (auditAckPayloadDic.ContainsKey("auths"))
                            {
                                Dictionary<string, object> auditAckAuthListDic = ConvertToDictionaryObject(auditAckPayloadDic["auths"]);
                                if (auditAckAuthListDic != null && auditAckAuthListDic.Count > 0)
                                {
                                    ack.AuthKeys = new Dictionary<string, PNAccessManagerKeyData>();

                                    foreach (string authKey in auditAckAuthListDic.Keys)
                                    {
                                        Dictionary<string, object> authDataDic = ConvertToDictionaryObject(auditAckAuthListDic[authKey]);
                                        if (authDataDic != null && authDataDic.Count > 0)
                                        {
                                            PNAccessManagerKeyData authData = new PNAccessManagerKeyData();
                                            authData.ReadEnabled = authDataDic["r"].ToString() == "1";
                                            authData.WriteEnabled = authDataDic["w"].ToString() == "1";
                                            authData.ManageEnabled = authDataDic.ContainsKey("m") ? authDataDic["m"].ToString() == "1" : false;
                                            authData.DeleteEnabled = authDataDic.ContainsKey("d") ? authDataDic["d"].ToString() == "1" : false;

                                            ack.AuthKeys.Add(authKey, authData);
                                        }
                                    }
                                }
                            }

                        }
                    }
                }

                ret = (T)Convert.ChangeType(ack, typeof(PNAccessManagerAuditResult), CultureInfo.InvariantCulture);
#endregion
            }
            else if (typeof(T) == typeof(PNPublishResult))
            {
#region "PNPublishResult"
                PNPublishResult result = null;
                if (listObject.Count >= 2)
                {
                    long publishTimetoken;
                    Int64.TryParse(listObject[2].ToString(), out publishTimetoken);
                    result = new PNPublishResult
                    {
                        Timetoken = publishTimetoken
                    };
                }

                ret = (T)Convert.ChangeType(result, typeof(PNPublishResult), CultureInfo.InvariantCulture);
#endregion
            }
            else if (typeof(T) == typeof(PNPresenceEventResult))
            {
#region "PNPresenceEventResult"
                Dictionary<string, object> presenceDicObj = ConvertToDictionaryObject(listObject[0]);

                PNPresenceEventResult ack = null;

                if (presenceDicObj != null)
                {
                    ack = new PNPresenceEventResult();
                    ack.Event = presenceDicObj["action"].ToString();
                    long presenceTimeStamp;
                    if (Int64.TryParse(presenceDicObj["timestamp"].ToString(), out presenceTimeStamp)){
                        ack.Timestamp = presenceTimeStamp;
                    }
                    if (presenceDicObj.ContainsKey("uuid"))
                    {
                        ack.Uuid = presenceDicObj["uuid"].ToString();
                    }
                    int presenceOccupany;
                    if (Int32.TryParse(presenceDicObj["occupancy"].ToString(), out presenceOccupany))
                    {
                        ack.Occupancy = presenceOccupany;
                    }

                    if (presenceDicObj.ContainsKey("data"))
                    {
                        Dictionary<string, object> stateDic = presenceDicObj["data"] as Dictionary<string, object>;
                        if (stateDic != null)
                        {
                            ack.State = stateDic;
                        }
                    }

                    long presenceTimetoken;
                    if (Int64.TryParse(listObject[2].ToString(), out presenceTimetoken))
                    {
                        ack.Timetoken = presenceTimetoken;
                    }
                    ack.Channel = (listObject.Count == 6) ? listObject[5].ToString() : listObject[4].ToString();
                    ack.Channel = ack.Channel.Replace("-pnpres", "");

                    if (listObject.Count == 6)
                    {
                        ack.Subscription = listObject[4].ToString();
                        ack.Subscription = ack.Subscription.Replace("-pnpres", "");
                    }

                    if (listObject[1] != null)
                    {
                        ack.UserMetadata = listObject[1];
                    }

                    if (ack.Event != null && ack.Event.ToLower() == "interval")
                    {
                        if (presenceDicObj.ContainsKey("join"))
                        {
                            List<object> joinDeltaList = presenceDicObj["join"] as List<object>;
                            if (joinDeltaList != null && joinDeltaList.Count > 0)
                            {
                                ack.Join = joinDeltaList.Select(x => x.ToString()).ToArray();
                            }
                        }
                        if (presenceDicObj.ContainsKey("timeout"))
                        {
                            List<object> timeoutDeltaList = presenceDicObj["timeout"] as List<object>;
                            if (timeoutDeltaList != null && timeoutDeltaList.Count > 0)
                            {
                                ack.Timeout = timeoutDeltaList.Select(x => x.ToString()).ToArray();
                            }
                        }
                        if (presenceDicObj.ContainsKey("leave"))
                        {
                            List<object> leaveDeltaList = presenceDicObj["leave"] as List<object>;
                            if (leaveDeltaList != null && leaveDeltaList.Count > 0)
                            {
                                ack.Leave = leaveDeltaList.Select(x => x.ToString()).ToArray();
                            }
                        }
                        if (presenceDicObj.ContainsKey("here_now_refresh"))
                        {
                            string hereNowRefreshStr = presenceDicObj["here_now_refresh"].ToString();
                            if (!string.IsNullOrEmpty(hereNowRefreshStr))
                            {
                                bool boolHereNowRefresh = false;
                                if (Boolean.TryParse(hereNowRefreshStr, out boolHereNowRefresh))
                                {
                                    ack.HereNowRefresh = boolHereNowRefresh;
                                }
                            }
                        }

                    }

                }

                ret = (T)Convert.ChangeType(ack, typeof(PNPresenceEventResult), CultureInfo.InvariantCulture);
#endregion
            }
            else if (typeof(T) == typeof(PNHistoryResult))
            {
#region "PNHistoryResult"
                PNHistoryResult result = PNHistoryJsonDataParse.GetObject(listObject);
                ret = (T)Convert.ChangeType(result, typeof(PNHistoryResult), CultureInfo.InvariantCulture);
#endregion
            }
            else if (typeof(T) == typeof(PNFetchHistoryResult))
            {
#region "PNFetchHistoryResult"
                PNFetchHistoryResult result = PNFetchHistoryJsonDataParse.GetObject(listObject);
                ret = (T)Convert.ChangeType(result, typeof(PNFetchHistoryResult), CultureInfo.InvariantCulture);
#endregion
            }
            else if (typeof(T) == typeof(PNDeleteMessageResult))
            {
#region "PNDeleteMessageResult"
                PNDeleteMessageResult ack = new PNDeleteMessageResult();
                ret = (T)Convert.ChangeType(ack, typeof(PNDeleteMessageResult), CultureInfo.InvariantCulture);
#endregion
            }
            else if (typeof(T) == typeof(PNMessageCountResult))
            {
                #region "PNMessageCountResult"
                PNMessageCountResult ack = null;
                Dictionary<string, object> messageCouuntContainerDicObj = ConvertToDictionaryObject(listObject[0]);
                if (messageCouuntContainerDicObj != null && messageCouuntContainerDicObj.ContainsKey("channels"))
                {
                    ack = new PNMessageCountResult();
                    Dictionary<string, object> messageCountDic = ConvertToDictionaryObject(messageCouuntContainerDicObj["channels"]);
                    if (messageCountDic != null)
                    {
                        ack.Channels = new Dictionary<string, long>();
                        foreach (string channel in messageCountDic.Keys)
                        {
                            long msgCount=0;
                            if (Int64.TryParse(messageCountDic[channel].ToString(), out msgCount))
                            {
                                ack.Channels.Add(channel, msgCount);
                            }
                        }
                    }
                }
                ret = (T)Convert.ChangeType(ack, typeof(PNMessageCountResult), CultureInfo.InvariantCulture);
                #endregion
            }
            else if (typeof(T) == typeof(PNHereNowResult))
            {
#region "PNHereNowResult"
                Dictionary<string, object> herenowDicObj = ConvertToDictionaryObject(listObject[0]);

                PNHereNowResult hereNowResult = null;

                if (herenowDicObj != null)
                {
                    hereNowResult = new PNHereNowResult();

                    string hereNowChannelName = listObject[1].ToString();

                    if (herenowDicObj.ContainsKey("payload"))
                    {
                        Dictionary<string, object> hereNowPayloadDic = ConvertToDictionaryObject(herenowDicObj["payload"]);
                        if (hereNowPayloadDic != null && hereNowPayloadDic.Count > 0)
                        {
                            int hereNowTotalOccupancy;
                            int hereNowTotalChannel;
                            if (Int32.TryParse(hereNowPayloadDic["total_occupancy"].ToString(), out hereNowTotalOccupancy))
                            {
                                hereNowResult.TotalOccupancy = hereNowTotalOccupancy;
                            }
                            if (Int32.TryParse(hereNowPayloadDic["total_channels"].ToString(), out hereNowTotalChannel))
                            {
                                hereNowResult.TotalChannels = hereNowTotalChannel;
                            }
                            if (hereNowPayloadDic.ContainsKey("channels"))
                            {
                                Dictionary<string, object> hereNowChannelListDic = ConvertToDictionaryObject(hereNowPayloadDic["channels"]);
                                if (hereNowChannelListDic != null && hereNowChannelListDic.Count > 0)
                                {
                                    foreach (string channel in hereNowChannelListDic.Keys)
                                    {
                                        Dictionary<string, object> hereNowChannelItemDic = ConvertToDictionaryObject(hereNowChannelListDic[channel]);
                                        if (hereNowChannelItemDic != null && hereNowChannelItemDic.Count > 0)
                                        {
                                            PNHereNowChannelData channelData = new PNHereNowChannelData();
                                            channelData.ChannelName = channel;
                                            int hereNowOccupancy;
                                            if (Int32.TryParse(hereNowChannelItemDic["occupancy"].ToString(), out hereNowOccupancy))
                                            {
                                                channelData.Occupancy = hereNowOccupancy;
                                            }
                                            if (hereNowChannelItemDic.ContainsKey("uuids"))
                                            {
                                                object[] hereNowChannelUuidList = ConvertToObjectArray(hereNowChannelItemDic["uuids"]);
                                                if (hereNowChannelUuidList != null && hereNowChannelUuidList.Length > 0)
                                                {
                                                    List<PNHereNowOccupantData> uuidDataList = new List<PNHereNowOccupantData>();

                                                    for (int index = 0; index < hereNowChannelUuidList.Length; index++)
                                                    {
                                                        if (hereNowChannelUuidList[index].GetType() == typeof(string))
                                                        {
                                                            PNHereNowOccupantData uuidData = new PNHereNowOccupantData();
                                                            uuidData.Uuid = hereNowChannelUuidList[index].ToString();
                                                            uuidDataList.Add(uuidData);
                                                        }
                                                        else
                                                        {
                                                            Dictionary<string, object> hereNowChannelItemUuidsDic = ConvertToDictionaryObject(hereNowChannelUuidList[index]);
                                                            if (hereNowChannelItemUuidsDic != null && hereNowChannelItemUuidsDic.Count > 0)
                                                            {
                                                                PNHereNowOccupantData uuidData = new PNHereNowOccupantData();
                                                                uuidData.Uuid = hereNowChannelItemUuidsDic["uuid"].ToString();
                                                                if (hereNowChannelItemUuidsDic.ContainsKey("state"))
                                                                {
                                                                    uuidData.State = ConvertToDictionaryObject(hereNowChannelItemUuidsDic["state"]);
                                                                }
                                                                uuidDataList.Add(uuidData);
                                                            }
                                                        }
                                                    }
                                                    channelData.Occupants = uuidDataList;
                                                }
                                            }
                                            hereNowResult.Channels.Add(channel, channelData);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else if (herenowDicObj.ContainsKey("occupancy"))
                    {
                        int hereNowTotalOccupancy;
                        if (Int32.TryParse(herenowDicObj["occupancy"].ToString(), out hereNowTotalOccupancy))
                        {
                            hereNowResult.TotalOccupancy = hereNowTotalOccupancy;
                        }
                        hereNowResult.Channels = new Dictionary<string, PNHereNowChannelData>();
                        if (herenowDicObj.ContainsKey("uuids"))
                        {
                            object[] uuidArray = ConvertToObjectArray(herenowDicObj["uuids"]);
                            if (uuidArray != null && uuidArray.Length > 0)
                            {
                                List<PNHereNowOccupantData> uuidDataList = new List<PNHereNowOccupantData>();
                                for (int index = 0; index < uuidArray.Length; index++)
                                {
                                    Dictionary<string, object> hereNowChannelItemUuidsDic = ConvertToDictionaryObject(uuidArray[index]);
                                    if (hereNowChannelItemUuidsDic != null && hereNowChannelItemUuidsDic.Count > 0)
                                    {
                                        PNHereNowOccupantData uuidData = new PNHereNowOccupantData();
                                        uuidData.Uuid = hereNowChannelItemUuidsDic["uuid"].ToString();
                                        if (hereNowChannelItemUuidsDic.ContainsKey("state"))
                                        {
                                            uuidData.State = ConvertToDictionaryObject(hereNowChannelItemUuidsDic["state"]);
                                        }
                                        uuidDataList.Add(uuidData);
                                    }
                                    else
                                    {
                                        PNHereNowOccupantData uuidData = new PNHereNowOccupantData();
                                        uuidData.Uuid = uuidArray[index].ToString();
                                        uuidDataList.Add(uuidData);
                                    }
                                }

                                PNHereNowChannelData channelData = new PNHereNowChannelData();
                                channelData.ChannelName = hereNowChannelName;
                                channelData.Occupants = uuidDataList;
                                channelData.Occupancy = hereNowResult.TotalOccupancy;

                                hereNowResult.Channels.Add(hereNowChannelName, channelData);
                                hereNowResult.TotalChannels = hereNowResult.Channels.Count;
                            }
                        }
                        else
                        {
                            string channels = listObject[1].ToString();
                            string[] arrChannel = channels.Split(',');
                            int totalChannels = 0;
                            foreach (string channel in arrChannel)
                            {
                                PNHereNowChannelData channelData = new PNHereNowChannelData();
                                channelData.Occupancy = 1;
                                hereNowResult.Channels.Add(channel, channelData);
                                totalChannels++;
                            }
                            hereNowResult.TotalChannels = totalChannels;


                        }
                    }

                }

                ret = (T)Convert.ChangeType(hereNowResult, typeof(PNHereNowResult), CultureInfo.InvariantCulture);
#endregion
            }
            else if (typeof(T) == typeof(PNWhereNowResult))
            {
#region "WhereNowAck"
                Dictionary<string, object> wherenowDicObj = ConvertToDictionaryObject(listObject[0]);

                PNWhereNowResult ack = null;

                if (wherenowDicObj != null)
                {
                    ack = new PNWhereNowResult();

                    if (wherenowDicObj.ContainsKey("payload"))
                    {
                        Dictionary<string, object> whereNowPayloadDic = ConvertToDictionaryObject(wherenowDicObj["payload"]);
                        if (whereNowPayloadDic != null && whereNowPayloadDic.Count > 0)
                        {
                            if (whereNowPayloadDic.ContainsKey("channels"))
                            {
                                object[] whereNowChannelList = ConvertToObjectArray(whereNowPayloadDic["channels"]);
                                if (whereNowChannelList != null && whereNowChannelList.Length >= 0)
                                {
                                    List<string> channelList = new List<string>();
                                    foreach (string channel in whereNowChannelList)
                                    {
                                        channelList.Add(channel);
                                    }
                                    ack.Channels = channelList;
                                }

                            }
                        }
                    }
                }

                ret = (T)Convert.ChangeType(ack, typeof(PNWhereNowResult), CultureInfo.InvariantCulture);
#endregion
            }
            else if (typeof(T) == typeof(PNSetStateResult))
            {
#region "SetUserStateAck"
                Dictionary<string, object> setUserStatewDicObj = ConvertToDictionaryObject(listObject[0]);

                PNSetStateResult ack = null;

                if (setUserStatewDicObj != null)
                {
                    ack = new PNSetStateResult();

                    ack.State = new Dictionary<string, object>();

                    if (setUserStatewDicObj.ContainsKey("payload"))
                    {
                        Dictionary<string, object> setStateDic = ConvertToDictionaryObject(setUserStatewDicObj["payload"]);
                        if (setStateDic != null)
                        {
                            ack.State = setStateDic;
                        }
                    }

                }

                ret = (T)Convert.ChangeType(ack, typeof(PNSetStateResult), CultureInfo.InvariantCulture);
#endregion
            }
            else if (typeof(T) == typeof(PNGetStateResult))
            {
#region "PNGetStateResult"
                Dictionary<string, object> getUserStatewDicObj = ConvertToDictionaryObject(listObject[0]);

                PNGetStateResult ack = null;

                if (getUserStatewDicObj != null)
                {
                    ack = new PNGetStateResult();

                    ack.StateByUUID = new Dictionary<string, object>();

                    if (getUserStatewDicObj.ContainsKey("payload"))
                    {
                        Dictionary<string, object> getStateDic = ConvertToDictionaryObject(getUserStatewDicObj["payload"]);
                        if (getStateDic != null)
                        {
                            ack.StateByUUID = getStateDic;
                        }
                    }
                }

                ret = (T)Convert.ChangeType(ack, typeof(PNGetStateResult), CultureInfo.InvariantCulture);
#endregion
            }
            else if (typeof(T) == typeof(PNChannelGroupsAllChannelsResult))
            {
#region "PNChannelGroupsAllChannelsResult"
                Dictionary<string, object> getCgChannelsDicObj = ConvertToDictionaryObject(listObject[0]);

                PNChannelGroupsAllChannelsResult ack = null;

                if (getCgChannelsDicObj != null)
                {
                    ack = new PNChannelGroupsAllChannelsResult();
                    Dictionary<string, object> getCgChannelPayloadDic = ConvertToDictionaryObject(getCgChannelsDicObj["payload"]);
                    if (getCgChannelPayloadDic != null && getCgChannelPayloadDic.Count > 0)
                    {
                        ack.ChannelGroup = getCgChannelPayloadDic["group"].ToString();
                        object[] channelGroupChPayloadChannels = ConvertToObjectArray(getCgChannelPayloadDic["channels"]);
                        if (channelGroupChPayloadChannels != null && channelGroupChPayloadChannels.Length > 0)
                        {
                            List<string> channelList = new List<string>();
                            for (int index = 0; index < channelGroupChPayloadChannels.Length; index++)
                            {
                                channelList.Add(channelGroupChPayloadChannels[index].ToString());
                            }
                            ack.Channels = channelList;
                        }
                    }
                }

                ret = (T)Convert.ChangeType(ack, typeof(PNChannelGroupsAllChannelsResult), CultureInfo.InvariantCulture);
#endregion
            }
            else if (typeof(T) == typeof(PNChannelGroupsListAllResult))
            {
#region "PNChannelGroupsListAllResult"
                Dictionary<string, object> getAllCgDicObj = ConvertToDictionaryObject(listObject[0]);

                PNChannelGroupsListAllResult ack = null;

                if (getAllCgDicObj != null)
                {
                    ack = new PNChannelGroupsListAllResult();

                    Dictionary<string, object> getAllCgPayloadDic = ConvertToDictionaryObject(getAllCgDicObj["payload"]);
                    if (getAllCgPayloadDic != null && getAllCgPayloadDic.Count > 0)
                    {
                        object[] channelGroupAllCgPayloadChannels = ConvertToObjectArray(getAllCgPayloadDic["groups"]);
                        if (channelGroupAllCgPayloadChannels != null && channelGroupAllCgPayloadChannels.Length > 0)
                        {
                            List<string> allCgList = new List<string>();
                            for (int index = 0; index < channelGroupAllCgPayloadChannels.Length; index++)
                            {
                                allCgList.Add(channelGroupAllCgPayloadChannels[index].ToString());
                            }
                            ack.Groups = allCgList;
                        }
                    }
                }

                ret = (T)Convert.ChangeType(ack, typeof(PNChannelGroupsListAllResult), CultureInfo.InvariantCulture);
#endregion
            }
            else if (typeof(T) == typeof(PNChannelGroupsAddChannelResult))
            {
#region "AddChannelToChannelGroupAck"
                Dictionary<string, object> addChToCgDicObj = ConvertToDictionaryObject(listObject[0]);

                PNChannelGroupsAddChannelResult ack = null;

                if (addChToCgDicObj != null)
                {
                    ack = new PNChannelGroupsAddChannelResult();
                }

                ret = (T)Convert.ChangeType(ack, typeof(PNChannelGroupsAddChannelResult), CultureInfo.InvariantCulture);
#endregion
            }
            else if (typeof(T) == typeof(PNChannelGroupsRemoveChannelResult))
            {
#region "PNChannelGroupsRemoveChannelResult"
                Dictionary<string, object> removeChFromCgDicObj = ConvertToDictionaryObject(listObject[0]);

                PNChannelGroupsRemoveChannelResult ack = null;

                int statusCode = 0;

                if (removeChFromCgDicObj != null)
                {
                    ack = new PNChannelGroupsRemoveChannelResult();

                    if (int.TryParse(removeChFromCgDicObj["status"].ToString(), out statusCode))
                    {
                        ack.Status = statusCode;
                    }

                    ack.Message = removeChFromCgDicObj["message"].ToString();
                    ack.Service = removeChFromCgDicObj["service"].ToString();

                    ack.Error = Convert.ToBoolean(removeChFromCgDicObj["error"].ToString());

                    ack.ChannelGroup = listObject[1].ToString();
                }

                ret = (T)Convert.ChangeType(ack, typeof(PNChannelGroupsRemoveChannelResult), CultureInfo.InvariantCulture);
#endregion
            }
            else if (typeof(T) == typeof(PNChannelGroupsDeleteGroupResult))
            {
#region "PNChannelGroupsDeleteGroupResult"
                Dictionary<string, object> removeCgDicObj = ConvertToDictionaryObject(listObject[0]);

                PNChannelGroupsDeleteGroupResult ack = null;

                int statusCode = 0;

                if (removeCgDicObj != null)
                {
                    ack = new PNChannelGroupsDeleteGroupResult();

                    if (int.TryParse(removeCgDicObj["status"].ToString(), out statusCode))
                    {
                        ack.Status = statusCode;
                    }

                    ack.Service = removeCgDicObj["service"].ToString();
                    ack.Message = removeCgDicObj["message"].ToString();

                    ack.Error = Convert.ToBoolean(removeCgDicObj["error"].ToString());
                }

                ret = (T)Convert.ChangeType(ack, typeof(PNChannelGroupsDeleteGroupResult), CultureInfo.InvariantCulture);
#endregion
            }
            else if (typeof(T) == typeof(PNTimeResult))
            {
#region "PNTimeResult"

                Int64 timetoken = 0;

                Int64.TryParse(listObject[0].ToString(), out timetoken);

                PNTimeResult result = new PNTimeResult
                {
                    Timetoken = timetoken
                };

                ret = (T)Convert.ChangeType(result, typeof(PNTimeResult), CultureInfo.InvariantCulture);
#endregion
            }
            else if (typeof(T) == typeof(PNPushAddChannelResult))
            {
#region "PNPushAddChannelResult"

                PNPushAddChannelResult result = new PNPushAddChannelResult();

                ret = (T)Convert.ChangeType(result, typeof(PNPushAddChannelResult), CultureInfo.InvariantCulture);
#endregion
            }
            else if (typeof(T) == typeof(PNPushListProvisionsResult))
            {
#region "PNPushListProvisionsResult"

                PNPushListProvisionsResult result = new PNPushListProvisionsResult();
                result.Channels = listObject.OfType<string>().Where(s => s.Trim() != "").ToList();

                ret = (T)Convert.ChangeType(result, typeof(PNPushListProvisionsResult), CultureInfo.InvariantCulture);
#endregion
            }
            else if (typeof(T) == typeof(PNPushRemoveChannelResult))
            {
#region "PNPushRemoveChannelResult"

                PNPushRemoveChannelResult result = new PNPushRemoveChannelResult();

                ret = (T)Convert.ChangeType(result, typeof(PNPushRemoveChannelResult), CultureInfo.InvariantCulture);
#endregion
            }
            else if (typeof(T) == typeof(PNPushRemoveAllChannelsResult))
            {
#region "PNPushRemoveAllChannelsResult"

                PNPushRemoveAllChannelsResult result = new PNPushRemoveAllChannelsResult();

                ret = (T)Convert.ChangeType(result, typeof(PNPushRemoveAllChannelsResult), CultureInfo.InvariantCulture);
#endregion
            }
            else if (typeof(T) == typeof(PNHeartbeatResult))
            {
#region "PNHeartbeatResult"
                Dictionary<string, object> heartbeatDicObj = ConvertToDictionaryObject(listObject[0]);
                PNHeartbeatResult result = null;

                if (heartbeatDicObj != null && heartbeatDicObj.ContainsKey("status"))
                {
                    result = new PNHeartbeatResult();

                    int statusCode;
                    if (int.TryParse(heartbeatDicObj["status"].ToString(), out statusCode))
                    {
                        result.Status = statusCode;
                    }

                    if (heartbeatDicObj.ContainsKey("message"))
                    {
                        result.Message = heartbeatDicObj["message"].ToString();
                    }
                }

                ret = (T)Convert.ChangeType(result, typeof(PNHeartbeatResult), CultureInfo.InvariantCulture);
#endregion
            }
            else if (typeof(T) == typeof(PNSetUuidMetadataResult))
            {
                #region "PNSetUuidMetadataResult"
                PNSetUuidMetadataResult result = PNSetUuidMetadataJsonDataParse.GetObject(listObject);
                ret = (T)Convert.ChangeType(result, typeof(PNSetUuidMetadataResult), CultureInfo.InvariantCulture);
                #endregion
            }
            else if (typeof(T) == typeof(PNRemoveUuidMetadataResult))
            {
                #region "PNDeleteUuidMetadataResult"
                PNRemoveUuidMetadataResult ack = new PNRemoveUuidMetadataResult();
                ret = (T)Convert.ChangeType(ack, typeof(PNRemoveUuidMetadataResult), CultureInfo.InvariantCulture);
                #endregion
            }
            else if (typeof(T) == typeof(PNGetAllUuidMetadataResult))
            {
                #region "PNGetAllUuidMetadataResult"
                PNGetAllUuidMetadataResult result = PNGetAllUuidMetadataJsonDataParse.GetObject(listObject);
                ret = (T)Convert.ChangeType(result, typeof(PNGetAllUuidMetadataResult), CultureInfo.InvariantCulture);
                #endregion
            }
            else if (typeof(T) == typeof(PNGetUuidMetadataResult))
            {
                #region "PNGetUuidMetadataResult"
                PNGetUuidMetadataResult result = PNGetUuidMetadataJsonDataParse.GetObject(listObject);
                ret = (T)Convert.ChangeType(result, typeof(PNGetUuidMetadataResult), CultureInfo.InvariantCulture);
                #endregion
            }
            else if (typeof(T) == typeof(PNSetChannelMetadataResult))
            {
                #region "PNSetChannelMetadataResult"
                PNSetChannelMetadataResult result = PNSetChannelMetadataJsonDataParse.GetObject(listObject);
                ret = (T)Convert.ChangeType(result, typeof(PNSetChannelMetadataResult), CultureInfo.InvariantCulture);
                #endregion
            }
            else if (typeof(T) == typeof(PNRemoveChannelMetadataResult))
            {
                #region "PNDeleteUserResult"
                PNRemoveChannelMetadataResult ack = new PNRemoveChannelMetadataResult();
                ret = (T)Convert.ChangeType(ack, typeof(PNRemoveChannelMetadataResult), CultureInfo.InvariantCulture);
                #endregion
            }
            else if (typeof(T) == typeof(PNGetAllChannelMetadataResult))
            {
                #region "PNGetSpacesResult"
                PNGetAllChannelMetadataResult result = PNGetAllChannelMetadataJsonDataParse.GetObject(listObject);
                ret = (T)Convert.ChangeType(result, typeof(PNGetAllChannelMetadataResult), CultureInfo.InvariantCulture);
                #endregion
            }
            else if (typeof(T) == typeof(PNGetChannelMetadataResult))
            {
                #region "PNGetSpaceResult"
                PNGetChannelMetadataResult result = PNGetChannelMetadataJsonDataParse.GetObject(listObject);
                ret = (T)Convert.ChangeType(result, typeof(PNGetChannelMetadataResult), CultureInfo.InvariantCulture);
                #endregion
            }
            else if (typeof(T) == typeof(PNMembershipsResult))
            {
                #region "PNMembershipsResult"
                PNMembershipsResult result = PNMembershipsJsonDataParse.GetObject(listObject);
                ret = (T)Convert.ChangeType(result, typeof(PNMembershipsResult), CultureInfo.InvariantCulture);
                #endregion
            }
            else if (typeof(T) == typeof(PNChannelMembersResult))
            {
                #region "PNChannelMembersResult"
                PNChannelMembersResult result = PNChannelMembersJsonDataParse.GetObject(listObject);
                ret = (T)Convert.ChangeType(result, typeof(PNChannelMembersResult), CultureInfo.InvariantCulture);
                #endregion
            }
            else if (typeof(T) == typeof(PNObjectEventResult))
            {
                #region "PNObjectEventResult"
                PNObjectEventResult result = PNObjectEventJsonDataParse.GetObject(listObject);
                ret = (T)Convert.ChangeType(result, typeof(PNObjectEventResult), CultureInfo.InvariantCulture);
                #endregion
            }
            else if (typeof(T) == typeof(PNMessageActionEventResult))
            {
                #region "PNMessageActionEventResult"
                PNMessageActionEventResult result = PNMessageActionEventJsonDataParse.GetObject(listObject);
                ret = (T)Convert.ChangeType(result, typeof(PNMessageActionEventResult), CultureInfo.InvariantCulture);
                #endregion
            }
            else if (typeof(T) == typeof(PNAddMessageActionResult))
            {
                #region "PNAddMessageActionResult"
                PNAddMessageActionResult result = PNAddMessageActionJsonDataParse.GetObject(listObject);
                ret = (T)Convert.ChangeType(result, typeof(PNAddMessageActionResult), CultureInfo.InvariantCulture);
                #endregion
            }
            else if (typeof(T) == typeof(PNRemoveMessageActionResult))
            {
                #region "PNRemoveMessageActionResult"
                PNRemoveMessageActionResult result = PNRemoveMessageActionJsonDataParse.GetObject(listObject);
                ret = (T)Convert.ChangeType(result, typeof(PNRemoveMessageActionResult), CultureInfo.InvariantCulture);
                #endregion
            }
            else if (typeof(T) == typeof(PNGetMessageActionsResult))
            {
                #region "PNGetMessageActionsResult"
                PNGetMessageActionsResult result = PNGetMessageActionsJsonDataParse.GetObject(listObject);
                ret = (T)Convert.ChangeType(result, typeof(PNGetMessageActionsResult), CultureInfo.InvariantCulture);
                #endregion
            }
            else if (typeof(T) == typeof(PNGenerateFileUploadUrlResult))
            {
                #region "PNGenerateFileUploadUrlResult"
                PNGenerateFileUploadUrlResult result = PNGenerateFileUploadUrlDataParse.GetObject(listObject);
                ret = (T)Convert.ChangeType(result, typeof(PNGenerateFileUploadUrlResult), CultureInfo.InvariantCulture);
                #endregion

            }
            else if (typeof(T) == typeof(PNPublishFileMessageResult))
            {
                #region "PNPublishFileMessageResult"
                PNPublishFileMessageResult result = PNPublishFileMessageJsonDataParse.GetObject(listObject);
                ret = (T)Convert.ChangeType(result, typeof(PNPublishFileMessageResult), CultureInfo.InvariantCulture);
                #endregion

            }
            else if (typeof(T) == typeof(PNListFilesResult))
            {
                #region "PNListFilesResult"
                PNListFilesResult result = PNListFilesJsonDataParse.GetObject(listObject);
                ret = (T)Convert.ChangeType(result, typeof(PNListFilesResult), CultureInfo.InvariantCulture);
                #endregion
            }
            else if (typeof(T) == typeof(PNDeleteFileResult))
            {
                #region "PNDeleteFileResult"
                PNDeleteFileResult ack = new PNDeleteFileResult();
                ret = (T)Convert.ChangeType(ack, typeof(PNDeleteFileResult), CultureInfo.InvariantCulture);
                #endregion

            }
            else
            {
                System.Diagnostics.Debug.WriteLine("DeserializeToObject<T>(list) => NO MATCH");
                try
                {
                    ret = (T)(object)listObject;
                }
                catch {  /* ignore */ }
            }

            return ret;
        }

        public Dictionary<string, object> DeserializeToDictionaryOfObject(string jsonString)
        {
            try
            {
                return JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonString);
            }
            catch
            {
                return null;
            }
        }

        public Dictionary<string, object> ConvertToDictionaryObject(object localContainer)
        {
            Dictionary<string, object> ret = null;

            try
            {
                if (localContainer != null)
                {
                    if (localContainer.GetType().ToString() == "Newtonsoft.Json.Linq.JObject")
                    {
                        ret = new Dictionary<string, object>();

                        IDictionary<string, JToken> jsonDictionary = localContainer as JObject;
                        if (jsonDictionary != null)
                        {
                            foreach (KeyValuePair<string, JToken> pair in jsonDictionary)
                            {
                                JToken token = pair.Value;
                                ret.Add(pair.Key, ConvertJTokenToObject(token));
                            }
                        }
                    }
                    else if (localContainer.GetType().ToString() == "System.Collections.Generic.Dictionary`2[System.String,System.Object]")
                    {
                        ret = new Dictionary<string, object>();
                        Dictionary<string, object> dictionary = localContainer as Dictionary<string, object>;
                        foreach (string key in dictionary.Keys)
                        {
                            ret.Add(key, dictionary[key]);
                        }
                    }
                    else if (localContainer.GetType().ToString() == "Newtonsoft.Json.Linq.JProperty")
                    {
                        ret = new Dictionary<string, object>();

                        JProperty jsonProp = localContainer as JProperty;
                        if (jsonProp != null)
                        {
                            string propName = jsonProp.Name;
                            ret.Add(propName, ConvertJTokenToObject(jsonProp.Value));
                        }
                    }
                    else if (localContainer.GetType().ToString() == "System.Collections.Generic.List`1[System.Object]")
                    {
                        List<object> localList = localContainer as List<object>;
                        if (localList != null)
                        {
                            if (localList.Count > 0 && localList[0].GetType() == typeof(KeyValuePair<string, object>))
                            {
                                ret = new Dictionary<string, object>();
                                foreach (object item in localList)
                                {
                                    if (item is KeyValuePair<string, object> kvpItem)
                                    {
                                        ret.Add(kvpItem.Key, kvpItem.Value);
                                    }
                                    else
                                    {
                                        ret = null;
                                        break;
                                    }
                                }
                            }
                            else if (localList.Count == 1 && localList[0].GetType() == typeof(Dictionary<string, object>))
                            {
                                ret = new Dictionary<string, object>();

                                Dictionary<string, object> localDic = localList[0] as Dictionary<string, object>;
                                foreach (object item in localDic)
                                {
                                    if (item is KeyValuePair<string, object> kvpItem)
                                    {
                                        ret.Add(kvpItem.Key, kvpItem.Value);
                                    }
                                    else
                                    {
                                        ret = null;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch { /* ignore */ }

            return ret;

        }

        public object[] ConvertToObjectArray(object localContainer)
        {
            object[] ret = null;

            try
            {
                if (localContainer.GetType().ToString() == "Newtonsoft.Json.Linq.JArray")
                {
                    JArray jarrayResult = localContainer as JArray;
                    List<object> objectContainer = jarrayResult.ToObject<List<object>>();
                    if (objectContainer != null && objectContainer.Count > 0)
                    {
                        for (int index = 0; index < objectContainer.Count; index++)
                        {
                            if (objectContainer[index].GetType().ToString() == "Newtonsoft.Json.Linq.JArray")
                            {
                                JArray internalItem = objectContainer[index] as JArray;
                                objectContainer[index] = internalItem.Select(item => (object)item).ToArray();
                            }
                        }
                        ret = objectContainer.ToArray<object>();
                    }
                }
                else if (localContainer.GetType().ToString() == "System.Collections.Generic.List`1[System.Object]")
                {
                    List<object> listResult = localContainer as List<object>;
                    ret = listResult.ToArray<object>();
                }
            }
            catch { /* ignore */ }

            return ret;
        }

        private static object ConvertJTokenToObject(JToken token)
        {
            if (token == null)
            {
                return null;
            }

            var jsonValue = token as JValue;
            if (jsonValue != null)
            {
                return jsonValue.Value;
            }

            var jsonContainer = token as JArray;
            if (jsonContainer != null)
            {
                List<object> jsonList = new List<object>();
                foreach (JToken arrayItem in jsonContainer)
                {
                    jsonList.Add(ConvertJTokenToObject(arrayItem));
                }
                return jsonList;
            }

            IDictionary<string, JToken> jsonObject = token as JObject;
            if (jsonObject != null)
            {
                var jsonDict = new Dictionary<string, object>();
                List<JProperty> propertyList = (from childToken in token
                                                where childToken is JProperty
                                                select childToken as JProperty).ToList();
                foreach (JProperty property in propertyList)
                {
                    jsonDict.Add(property.Name, ConvertJTokenToObject(property.Value));
                }

                return jsonDict;
            }

            return null;
        }

        private static object ConvertToDataType(Type dataType, object inputValue)
        {
            if (dataType == inputValue.GetType())
            {
                return inputValue;
            }

            object userMessage = inputValue;
            switch (dataType.FullName)
            {
                case "System.Int32":
                    userMessage = Convert.ChangeType(inputValue, typeof(System.Int32), CultureInfo.InvariantCulture);
                    break;
                case "System.Int16":
                    userMessage = Convert.ChangeType(inputValue, typeof(System.Int16), CultureInfo.InvariantCulture);
                    break;
                case "System.UInt64":
                    userMessage = Convert.ChangeType(inputValue, typeof(System.UInt64), CultureInfo.InvariantCulture);
                    break;
                case "System.UInt32":
                    userMessage = Convert.ChangeType(inputValue, typeof(System.UInt32), CultureInfo.InvariantCulture);
                    break;
                case "System.UInt16":
                    userMessage = Convert.ChangeType(inputValue, typeof(System.UInt16), CultureInfo.InvariantCulture);
                    break;
                case "System.Byte":
                    userMessage = Convert.ChangeType(inputValue, typeof(System.Byte), CultureInfo.InvariantCulture);
                    break;
                case "System.SByte":
                    userMessage = Convert.ChangeType(inputValue, typeof(System.SByte), CultureInfo.InvariantCulture);
                    break;
                case "System.Decimal":
                    userMessage = Convert.ChangeType(inputValue, typeof(System.Decimal), CultureInfo.InvariantCulture);
                    break;
                case "System.Boolean":
                    userMessage = Convert.ChangeType(inputValue, typeof(System.Boolean), CultureInfo.InvariantCulture);
                    break;
                case "System.Double":
                    userMessage = Convert.ChangeType(inputValue, typeof(System.Double), CultureInfo.InvariantCulture);
                    break;
                case "System.Char":
                    userMessage = Convert.ChangeType(inputValue, typeof(System.Char), CultureInfo.InvariantCulture);
                    break;
                case "System.String":
                    userMessage = Convert.ChangeType(inputValue, typeof(System.String), CultureInfo.InvariantCulture);
                    break;
                case "System.Object":
                    userMessage = Convert.ChangeType(inputValue, typeof(System.Object), CultureInfo.InvariantCulture);
                    break;
                default:
                    break;
            }

            return userMessage;
        }

#endregion

    }

}
