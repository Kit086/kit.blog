import { remark } from 'remark';
import html from 'remark-html';
import { visit } from 'unist-util-visit';
import remarkToc from 'remark-toc';
import remarkGfm from 'remark-gfm';
import remarkSlug from 'remark-slug';

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
  // 在文件开头添加目录标记
  const contentWithToc = markdown.includes('[TOC]')
    ? markdown.replace('[TOC]', '## Table of Contents')
    : '## Table of Contents\n\n' + markdown;

  const result = await remark()
    .use(remarkSlug)  // 添加标题的 id
    .use(remarkToc, {
      heading: 'Table of Contents',
      tight: true,
      maxDepth: 3,
    })
    .use(remarkGfm)   // GitHub Flavored Markdown
    .use(createImagePathPlugin(slug))
    .use(html, { sanitize: false })
    .process(contentWithToc);

  return result.toString();
}
