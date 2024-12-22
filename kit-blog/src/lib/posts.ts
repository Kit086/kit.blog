import fs from 'fs';
import path from 'path';
import matter from 'gray-matter';
import { markdownToHtml } from './markdownToHtml';
import { Post } from '@/interfaces/Post';

const postsDirectory = path.join(process.cwd(), '_posts');

export async function getPostBySlug(slug: string): Promise<Post> {
  const postDir = path.join(postsDirectory, slug);
  const files = fs.readdirSync(postDir);
  const mdFile = files.find(file => file.endsWith('.md'));
  
  if (!mdFile) {
    throw new Error(`No markdown file found for slug: ${slug}`);
  }

  const fullPath = path.join(postDir, mdFile);
  const fileContents = fs.readFileSync(fullPath, 'utf8');

  try {
    // 使用 gray-matter 解析 frontmatter
    const { data, content } = matter(fileContents);

    // 转换 markdown 为 HTML
    const contentHtml = await markdownToHtml(content, slug);

    return {
      ...data as Omit<Post, 'contentHtml'>,
      contentHtml,
    };
  } catch (error) {
    if (error instanceof Error) {
      throw new Error(`Error parsing markdown file ${fullPath}: ${error.message}`);
    } else {
      throw new Error(`Error parsing markdown file ${fullPath}: ${String(error)}`);
    }
  }
}

export function getAllPostSlugs(): string[] {
  try {
    // 确保目录存在
    if (!fs.existsSync(postsDirectory)) {
      console.warn('Posts directory does not exist:', postsDirectory);
      return [];
    }

    // 获取所有文章目录
    return fs.readdirSync(postsDirectory).filter(dir => {
      const fullPath = path.join(postsDirectory, dir);
      // 确保是目录且包含 markdown 文件
      return (
        fs.statSync(fullPath).isDirectory() &&
        fs.readdirSync(fullPath).some(file => file.endsWith('.md'))
      );
    });
  } catch (error) {
    console.error('Error getting post slugs:', error);
    return [];
  }
}

export function getAllPosts(): Post[] {
  const postDirs = getAllPostSlugs();
  const posts = postDirs.map((dir) => {
    try {
      const fullPath = path.join(postsDirectory, dir);
      const files = fs.readdirSync(fullPath);
      const mdFile = files.find(file => file.endsWith('.md'));
      
      if (!mdFile) {
        console.warn(`No markdown file found in directory: ${dir}`);
        return null;
      }

      const fileContents = fs.readFileSync(path.join(fullPath, mdFile), 'utf8');
      const { data } = matter(fileContents);

      return {
        ...data,
        slug: dir,
      } as Post;
    } catch (error) {
      console.error(`Error processing post in directory ${dir}:`, error);
      return null;
    }
  }).filter((post): post is Post => post !== null);

  // 按创建时间排序
  return posts.sort((a, b) => 
    new Date(b.create_time).getTime() - new Date(a.create_time).getTime()
  );
}
