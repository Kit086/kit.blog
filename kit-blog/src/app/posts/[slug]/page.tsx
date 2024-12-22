import { getPostBySlug, getAllPostSlugs } from '@/lib/posts';
import { notFound } from 'next/navigation';
import type { Post } from '@/interfaces/Post';

type Params = {
  params: {
    slug: string | string[];
  };
};

export async function generateStaticParams() {
  const slugs = getAllPostSlugs();
  return slugs.map((slug) => ({
    slug,
  }));
}

export default async function PostPage(props: Params) {
  const resolvedParams = await Promise.resolve(props.params);
  const slug = resolvedParams.slug;

  // 如果 slug 是数组或不是字符串，返回 404
  if (Array.isArray(slug) || typeof slug !== 'string') {
    notFound();
  }

  try {
    const post: Post = await getPostBySlug(slug);

    return (
      <article className="prose prose-sm sm:prose-base lg:prose-lg xl:prose-xl 2xl:prose-2xl dark:prose-invert mx-auto">
        <h1>{post.title}</h1>
        <div className="text-sm text-gray-500">
          <time dateTime={post.create_time}>
            {new Date(post.create_time).toLocaleDateString()}
          </time>
          {post.tags && post.tags.length > 0 && (
            <div className="mt-2">
              {post.tags.map((tag) => (
                <span key={tag} className="mr-2 px-2 py-1 bg-gray-100 rounded-md">
                  {tag}
                </span>
              ))}
            </div>
          )}
        </div>
        <div className="mt-8" dangerouslySetInnerHTML={{ __html: post.contentHtml || '' }} />
      </article>
    );
  } catch (error) {
    console.error('Error loading post:', error);
    notFound();
  }
}
