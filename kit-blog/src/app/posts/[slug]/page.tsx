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
      <article className="prose prose-sm sm:prose-base lg:prose-lg xl:prose-xl 2xl:prose-2xl mx-auto">
        <div className="not-prose">
          <h1 className="text-4xl font-bold text-primary-content mb-4">{post.title}</h1>
          <div className="flex flex-wrap gap-4 items-center text-primary-content/70 mb-8">
            <time dateTime={post.create_time} className="text-sm">
              {new Date(post.create_time).toLocaleDateString()}
            </time>
            {post.tags && post.tags.length > 0 && (
              <div className="flex flex-wrap gap-2">
                {post.tags.map((tag) => (
                  <span key={tag} className="badge badge-outline">
                    {tag}
                  </span>
                ))}
              </div>
            )}
          </div>
        </div>

        <div 
          className="mt-8 prose-headings:text-primary-content prose-p:text-primary-content/90 
                     prose-strong:text-primary-content prose-em:text-primary-content/90
                     prose-code:text-primary-content prose-code:bg-base-200 prose-code:px-1.5 prose-code:py-0.5 prose-code:rounded
                     prose-a:text-primary hover:prose-a:text-primary/80
                     prose-blockquote:text-primary-content/75 prose-blockquote:border-primary-content/20
                     prose-li:text-primary-content/90
                     [&_table]:bg-base-100 prose-th:text-primary-content prose-td:text-primary-content/90
                     [&_pre]:bg-base-200 [&_pre_code]:bg-transparent
                     dark:prose-invert" 
          dangerouslySetInnerHTML={{ __html: post.contentHtml || '' }} 
        />
      </article>
    );
  } catch (error) {
    console.error('Error loading post:', error);
    notFound();
  }
}
