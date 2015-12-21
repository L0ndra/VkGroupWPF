using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;
using System.Net;
namespace VkGroupWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        long Group1;
        long Group2;
        string Token;
        int GroupCount;
        class Group
        {
            public long GroupId { get; set; }
            public string GroupName { get; set; }
        }
        List<Group> GroupList;
        Dictionary<long, MemberStats> MemberList, CommonMember;
        class MemberStats
        {
            public long Id { get; set; }
            public string Name { get; set; }
            public int LikeSet { get; set; }
            public int LikeGet { get; set; }
            public int Comments { get; set; }
            public int Reposts { get; set; }
            public int Score { get; set; }
            public MemberStats(long id, string name)
            {
                Id = id;
                Name = name;
                LikeSet = 0;
                LikeGet = 0;
                Comments = 0;
                Reposts = 0;
                Score = 0;

            }
        }
        public MainWindow()
        {
            InitializeComponent();
            webBrowser.Visibility = Visibility.Visible;
            webBrowser.Navigate("https://oauth.vk.com/authorize?client_id=5141520&scope=groups&redirect_uri=https://oauth.vk.com/blank.html&display=page&v=5.41&response_type=token");
            webBrowser.LoadCompleted += WebBrowser_LoadCompleted;
            button.Click += Button_Click;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            int idx = comboBox.SelectedIndex;
            Group1 = GroupList[idx].GroupId;

            idx = comboBox1.SelectedIndex;
            Group2 = GroupList[idx].GroupId;
            Common();
            GetStats();
            MemberList = new Dictionary<long, MemberStats>();
            foreach (var item in CommonMember.OrderByDescending(i => i.Value.Score))
            {
                MemberList.Add(item.Key, item.Value);
            }
            listView.ItemsSource = MemberList.Values;
        }
        void Common()
        {
            MemberList = new Dictionary<long, MemberStats>();
            CommonMember = new Dictionary<long, MemberStats>();
            XmlDocument result;
            int offset = 0;
            result = MakeRequest("https://api.vk.com/method/groups.getMembers.xml?extended=1&fields=name&offset=0&count=1000&v=5.41&group_id=" + Convert.ToString(Group1));
            while (result.ChildNodes[1].ChildNodes[1].ChildNodes.Count != 0)
            {
                for (int i = 0; i < result.ChildNodes[1].ChildNodes[1].ChildNodes.Count; i++)
                {
                    XmlNodeList node = result.ChildNodes[1].ChildNodes[1].ChildNodes[i].ChildNodes;
                    long id = Convert.ToInt64(node[0].InnerText);
                    string name = node[2].InnerText + " " + node[1].InnerText;
                    MemberStats ms = new MemberStats(id, name);
                    MemberList.Add(id, ms);
                }
                offset += 1000;
                result = MakeRequest("https://api.vk.com/method/groups.getMembers.xml?extended=1&fields=name&offset=" + Convert.ToString(offset) + "&count=1000&v=5.41&group_id=" + Convert.ToString(Group1));
            }
            offset = 0;
            result = MakeRequest("https://api.vk.com/method/groups.getMembers.xml?extended=1&fields=name&offset=0&count=1000&v=5.41&group_id=" + Convert.ToString(Group2));

            while (result.ChildNodes[1].ChildNodes[1].ChildNodes.Count != 0)
            {
                for (int i = 0; i < result.ChildNodes[1].ChildNodes[1].ChildNodes.Count; i++)
                {
                    XmlNodeList node = result.ChildNodes[1].ChildNodes[1].ChildNodes[i].ChildNodes;
                    long id = Convert.ToInt64(node[0].InnerText);
                    string name = node[2].InnerText + " " + node[1].InnerText;
                    MemberStats ms = new MemberStats(id, name);

                    if (MemberList.ContainsKey(id))
                        CommonMember.Add(id, ms);
                }
                offset += 1000;
                result = MakeRequest("https://api.vk.com/method/groups.getMembers.xml?extended=1&fields=name&offset=" + Convert.ToString(offset) + "&count=1000&v=5.41&group_id=" + Convert.ToString(Group2));
            }

            textBlock.Text = Convert.ToString(CommonMember.Count);
        }

        private void WebBrowser_LoadCompleted(object sender, NavigationEventArgs e)
        {
            string token = webBrowser.Source.AbsoluteUri;
            string path = webBrowser.Source.AbsolutePath;
            if (path[1] == 'b')
            {
                token = token.Split('#')[1];
                if (token[0] == 'a')
                {
                    token = token.Split('&')[0];
                    token = token.Split('=')[1];
                    webBrowser.Visibility = Visibility.Hidden;
                    Token = token;

                    Load_Group();

                }
            }

        }
        XmlDocument MakeRequest(string requestUrl)
        {
            HttpWebRequest request = WebRequest.Create(requestUrl) as HttpWebRequest;
            HttpWebResponse response = request.GetResponse() as HttpWebResponse;
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(response.GetResponseStream());
            return xmlDoc;
        }
        private XmlDocument GetGroups()
        {
            XmlDocument result = MakeRequest("https://api.vk.com/method/groups.get.xml?extended=1&offset=0&count=1000&v=5.41&access_token=" + Token);
            return result;
        }
        void Load_Group()
        {
            XmlDocument groupsXml = GetGroups();
            XmlNodeList xml = groupsXml.ChildNodes[1].ChildNodes;
            GroupCount = Convert.ToInt32(xml[0].ChildNodes[0].Value);
            XmlNodeList groups = xml[1].ChildNodes;
            GroupList = new List<Group>();
            for (int i = 0; i < GroupCount; i++)
            {
                Group group = new Group();
                group.GroupId = Convert.ToInt32(groups[i].ChildNodes[0].InnerText);
                group.GroupName = groups[i].ChildNodes[1].InnerText;
                GroupList.Add(group);
                comboBox.Items.Add(group.GroupName);
                comboBox1.Items.Add(group.GroupName);
            }

        }
        void GetStats()
        {
            XmlDocument result = MakeRequest("https://api.vk.com/method/wall.get.xml?extended=0&&offset=0&count=100&v=5.41&owner_id=-" + Convert.ToString(Group1));
            XmlNodeList nodes = result.ChildNodes[1].ChildNodes[1].ChildNodes;
            foreach (XmlNode node in nodes)
            {
                long PostId = Convert.ToInt64(node.ChildNodes[0].InnerText);
                GetLikes(PostId, Group1);
                GetComments(PostId, Group1);
                GetReposts(PostId, Group1);
            }
            result = MakeRequest("https://api.vk.com/method/wall.get.xml?extended=0&&offset=0&count=100&v=5.41&owner_id=-" + Convert.ToString(Group2));
            nodes = result.ChildNodes[1].ChildNodes[1].ChildNodes;
            foreach (XmlNode node in nodes)
            {
                long PostId = Convert.ToInt64(node.ChildNodes[0].InnerText);
                GetLikes(PostId, Group2);
                GetComments(PostId, Group2);
                GetReposts(PostId, Group2);
            }
        }
        void GetLikes(long PostId, long GroupId)
        {
            int offset = 0;
            XmlDocument result = MakeRequest("https://api.vk.com/method/likes.getList.xml?type=post&count=1000&owner_id=-" + Convert.ToString(GroupId) + "&item_id=" + Convert.ToString(PostId) + "&offset=" + Convert.ToString(offset));
            XmlNodeList nodes = result.ChildNodes[1].ChildNodes[1].ChildNodes;
            while (nodes.Count > 0)
            {
                foreach (XmlNode node in nodes)
                {
                    long id = Convert.ToInt64(node.InnerText);
                    if (CommonMember.ContainsKey(id))
                    {
                        CommonMember[id].LikeSet++;
                        CommonMember[id].Score++;
                    }
                }
                offset += 1000;
                result = MakeRequest("https://api.vk.com/method/likes.getList.xml?type=post&count=1000&owner_id=-" + Convert.ToString(GroupId) + "&item_id=" + Convert.ToString(PostId) + "&offset=" + Convert.ToString(offset));
                nodes = result.ChildNodes[1].ChildNodes[1].ChildNodes;
            }

        }
        void GetComments(long PostId, long GroupId)
        {
            int offset = 0;
            XmlDocument result = MakeRequest("https://api.vk.com/method/wall.getComments.xml?need_likes=1&count=100&owner_id=-" + Convert.ToString(GroupId) + "&post_id=" + Convert.ToString(PostId) + "&offset=" + Convert.ToString(offset));
            if (Convert.ToInt32(result.ChildNodes[1].ChildNodes[0].InnerText) > 0)
            {
                XmlNodeList nodes = result.ChildNodes[1].ChildNodes;
                while (nodes.Count > 1)
                {

                    for (int i=1;i < nodes.Count;i++)
                    {
                        long id = Convert.ToInt64(nodes[i].ChildNodes[1].InnerText);
                        int likes = Convert.ToInt32(nodes[i].ChildNodes[5].InnerText);
                        if (CommonMember.ContainsKey(id))
                        {
                            CommonMember[id].Comments++;
                            CommonMember[id].LikeGet += likes;
                            CommonMember[id].Score += likes + 1;
                        }
                    }
                    offset += 100;
                    result = MakeRequest("https://api.vk.com/method/wall.getComments.xml?need_likes=1&count=100&owner_id=-" + Convert.ToString(GroupId) + "&post_id=" + Convert.ToString(PostId) + "&offset=" + Convert.ToString(offset));
                    nodes = result.ChildNodes[1].ChildNodes;
                }
            }
        }
        void GetReposts(long PostId, long GroupId)
        {
            int offset = 0;
            XmlDocument result = MakeRequest("https://api.vk.com/method/wall.getReposts.xml?count=1000&owner_id=-" + Convert.ToString(GroupId) + "&post_id=" + Convert.ToString(PostId) + "&offset=" + Convert.ToString(offset));
            XmlNodeList nodes = result.ChildNodes[1].ChildNodes[1].ChildNodes;
            while (nodes.Count > 0)
            {
                foreach (XmlNode node in nodes)
                {
                    long id = Convert.ToInt64(node.ChildNodes[0].InnerText);
                    if (CommonMember.ContainsKey(id))
                    {
                        CommonMember[id].Reposts++;
                        CommonMember[id].Score++;
                    }

                }
                offset += 1000;
                result = MakeRequest("https://api.vk.com/method/wall.getReposts.xml?count=1000&owner_id=-" + Convert.ToString(GroupId) + "&post_id=" + Convert.ToString(PostId) + "&offset=" + Convert.ToString(offset));
                nodes = result.ChildNodes[1].ChildNodes[1].ChildNodes;

            }
        }
    }
}
