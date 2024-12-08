import { remark } from 'remark';
import html from 'remark-html';
import { visit } from 'unist-util-visit';

function createImagePathPlugin(slug: string) {
  return () => (tree: any) => {
    visit(tree, 'image', (node: any) => {
      // 如果是绝对路径或外部链接，不做处理
      if (node.url.startsWith('http') || node.url.startsWith('/')) {
        return;
      }
      
      // 将相对路径转换为 /assets/[slug]/ 开头的路径
      node.url = `/assets/${slug}/${node.url}`;
    });
  };
}

export async function markdownToHtml(markdown: string, slug: string) {
  const result = await remark()
    .use(createImagePathPlugin(slug))
    .use(html)
    .process(markdown);
  return result.toString();
}
