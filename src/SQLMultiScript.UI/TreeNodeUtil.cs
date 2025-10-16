namespace SQLMultiScript.UI
{
    internal class TreeNodeUtil
    {
        public static IEnumerable<TreeNode> GetAllNodes(TreeNode node)
        {
            yield return node;

            foreach (TreeNode child in node.Nodes)
            {
                foreach (var sub in GetAllNodes(child))
                    yield return sub;
            }
        }

        

    }
}
