﻿using Masuit.MyBlogs.Core.Infrastructure.Repository.Interface;
using Masuit.MyBlogs.Core.Models.Entity;

namespace Masuit.MyBlogs.Core.Infrastructure.Repository;

public sealed partial class CommentRepository : BaseRepository<Comment>, ICommentRepository
{
}