using HtmlAgilityPack;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Xml;
using System.Xml.XPath;

namespace Sketch2Code
{
    class Program
    {

        public static List<TagsHierarchyModel> _tagsHierarchyList = new List<TagsHierarchyModel>();
        public static int indexCount = 0;
        public static int tempIndex = 0;

        
        static void Main(string[] args)
        {
            HtmlDocument htmlDoc = new HtmlDocument();

            var folderPath = @"C:\Users\hemant.naik\Downloads";
            var files = Directory.GetFiles(folderPath, "*.html");

            htmlDoc.Load(files[0]);

            var fileName = Path.GetFileName(files[0]).Split('.')[0].ToLower();

            GetTagsHierarchyDetails(htmlDoc);

            foreach (var item in _tagsHierarchyList)
            {
                string TagValue = item.TagValue == "" ? "NULL" : item.TagValue;
                Console.Write(item.ID + "---" + item.TagName + "---" + TagValue + "---" + item.Index);
                Console.WriteLine();
            }

            Console.WriteLine("Hello World!");
            ConvertToSWFControls(fileName);
            ConvertToViewModel(fileName);
        }

        //public static List<TagsHierarchyModel> GetTags()
        //{
        //    return new List<TagsHierarchyModel>()
        //    {
        //        new TagsHierarchyModel()
        //        {
        //            ID = 1,
        //            Index =0,
        //            TagName="label",
        //            TagValue = "name"
        //        },
        //        new TagsHierarchyModel()
        //        {
        //            ID = 2,
        //            Index =1,
        //            TagName="input",
        //            TagValue = ""
        //        },
        //        new TagsHierarchyModel()
        //        {
        //            ID = 3,
        //            Index =2,
        //            TagName="label",
        //            TagValue = "password"
        //        },
        //        new TagsHierarchyModel()
        //        {
        //            ID = 4,
        //            Index =3,
        //            TagName="input",
        //            TagValue = ""
        //        },
        //        new TagsHierarchyModel()
        //        {
        //            ID = 5,
        //            Index =4,
        //            TagName="button",
        //            TagValue = "submit"
        //        }
        //    };
        //}

        public static void ConvertToSWFControls(string fileName)
        {
            string content = File.ReadAllText(Path.GetFullPath("ViewTemplate.txt"));
            var fieldLeft = new StringBuilder();
            var fieldRight = new StringBuilder();
            var buttonBuilder = new StringBuilder();

            var leftControls = _tagsHierarchyList.Where(x => x.Indent == 0 && x.TagName != "button").ToList();
            var rightControls = _tagsHierarchyList.Where(x => x.Indent == 1).Select(c => { c.Index = c.Index - 1; return c; }).ToList();  
            var button = _tagsHierarchyList.Where(x => x.TagName == "button").ToList();  
          

            foreach (var item in leftControls)
            {
               GenerateControls(item, fieldLeft, leftControls);
            }
            foreach (var item in rightControls)
            {
              GenerateControls(item, fieldRight, rightControls);
            }
            foreach (var item in button)
            {
                GenerateControls(item, buttonBuilder, button);
            }

            var path = @"C:\repo\Hackathon2021\helloWorld\src\helloModule\src\html";

            var contentReplacedLeft = content.Replace("//insert1", fieldLeft.ToString());
            var contentReplacedRight = contentReplacedLeft.Replace("//insert2", fieldRight.ToString());
            var contentReplacedButton = contentReplacedRight.Replace("//insert3", buttonBuilder.ToString());

            File.WriteAllText($"{path}\\{fileName}View.html", contentReplacedButton);            

        }

        public static void GenerateControls(TagsHierarchyModel item, StringBuilder fields,IList<TagsHierarchyModel> tagsHierarchyModels)
        {
            var swfTagsJson = File.ReadAllText("swf-tag.json");
            var swfTags = JObject.Parse(swfTagsJson);
            var tagValue = item.TagValue;
            tagValue = tagValue.Replace(" ","");
            tagValue = tagValue.Replace("-","");
            if (item.TagName == "label" && !item.TagValue.Contains("Terms and Conditions"))
            {
                if (item.Index + 1 < tagsHierarchyModels.Count && tagsHierarchyModels[item.Index + 1].TagName == "input")
                {
                    fields.Append("\n");
                    fields.Append(string.Format(swfTags["SWF"].Value<string>("input"), $"{tagValue}"));
                    // continue;
                }
                else
                {
                    fields.Append("\n");
                    fields.Append(string.Format(swfTags["SWF"].Value<string>("label"), $"{tagValue}"));
                }

            }
            else if (item.TagName == "button")
            {
                fields.Append("\n");
                fields.Append(string.Format(swfTags["SWF"].Value<string>("button"), $"{tagValue}"));
            }

        }

        public static void ConvertToViewModel(string fileName)
        {
            var viewModelJson = File.ReadAllText("ViewModelTemplate.json");
            var viewModelTags = JObject.Parse(viewModelJson);
            var dataNew = new JObject();
            List<PropertyInfo> propertyInfos = new List<PropertyInfo>();

            foreach (var item in _tagsHierarchyList)
            {
                if (item.TagName == "label")
                {
                    var propertyInfo = new PropertyInfo()
                    {
                        displayName = item.TagValue,
                        type = "STRING",
                        isEditable = "true",
                        dbValue = "",
                        dispValue = ""
                    };

                    JToken objContent = JToken.FromObject(propertyInfo);
                    var value = item.TagValue;
                    value = value.Replace(" ","");
                    value = value.Replace("-", "");
                    JProperty jProperty = new JProperty(value, objContent);
                    dataNew.Add(jProperty);
                }

            }
            var path = @"C:\repo\Hackathon2021\helloWorld\src\helloModule\src\viewmodel";
            var contentReplaced = viewModelJson.Replace("//insert", $"\"data\":{dataNew.ToString()}");

            File.WriteAllText($"{path}\\{fileName}ViewModel.json", contentReplaced);
        }

        public static void GetTagsHierarchyDetails(HtmlDocument htmlDoc)
        {
            HtmlNode _htmlNode = htmlDoc.DocumentNode.SelectNodes("//body").FirstOrDefault();

            foreach (HtmlNode node in _htmlNode.ChildNodes)
            {
                checkNode(node);
            }
        }

        public static void checkNode(HtmlNode node)
        {

            foreach (HtmlNode _childNodes in node.ChildNodes)
            {
                if (_childNodes.HasChildNodes)
                {
                    checkNode(_childNodes);
                }
                else
                {
                    if ((_childNodes?.Attributes.Count > 0 && _childNodes?.Attributes[0]?.Value == "checkbox"))
                    {
                        // indexCount++;                        
                        continue;
                    }

                    if (_childNodes.ParentNode.Name != "div" || _childNodes.Name == "input")
                    {
                        var currentNode = _childNodes.ParentNode.Name == "div" ? _childNodes.Name : _childNodes.ParentNode.Name;

                        //if (indexCount > 0 && currentNode == "label" && (_tagsHierarchyList[indexCount + 1].TagName == "input"))
                        //{
                        //    if(true)
                        //    {

                        //    }
                        //}

                        TagsHierarchyModel _tagsHierarchyModel = new TagsHierarchyModel();
                        _tagsHierarchyModel.ID = indexCount;
                        _tagsHierarchyModel.TagName = _childNodes.ParentNode.Name == "div" ? _childNodes.Name : _childNodes.ParentNode.Name;
                        _tagsHierarchyModel.TagValue = _childNodes.InnerText;
                        _tagsHierarchyModel.Index = tempIndex;



                        if (indexCount > 0 && currentNode == "label" && (_tagsHierarchyList[indexCount - 1].TagName == "label"))
                        {
                            _tagsHierarchyModel.Indent = 1;
                            _tagsHierarchyModel.Index = --tempIndex;

                            //_tagsHierarchyList.Add(_tagsHierarchyModel);

                            //indexCount++;

                            //continue;
                        }

                        if (indexCount > 0 && _childNodes.Name == "input" && (_tagsHierarchyList[indexCount - 1].TagName == "input"))
                        {
                            _tagsHierarchyModel.Indent = 1;
                            _tagsHierarchyModel.Index = --tempIndex;

                            //_tagsHierarchyList.Add(_tagsHierarchyModel);

                            //indexCount++;

                            //continue;
                        }

                        _tagsHierarchyList.Add(_tagsHierarchyModel);

                        indexCount++;
                        tempIndex++;
                    }
                }
            }
        }
    }

    public class TagsHierarchyModel
    {
        public int ID;
        public string TagName { get; set; }
        public string TagValue { get; set; }
        public int Index { get; set; }
        public string AttributeType { get; set; }
        public int Indent { get; set; }
    }

    public class PropertyInfo
    {
        public string displayName { get; set; }
        public string type { get; set; }
        public string isEditable { get; set; }
        public string dbValue { get; set; }
        public string dispValue { get; set; }
    }

}
