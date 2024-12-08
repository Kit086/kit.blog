export interface Post {
  title: string;
  slug: string;
  create_time: string;
  last_updated: string;
  description: string;
  tags: string[];
  contentHtml?: string;  // 可选属性，用于存储转换后的 HTML 内容
}
