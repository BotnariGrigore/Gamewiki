CREATE DATABASE IF NOT EXISTS game_wiki_platform;
USE game_wiki_platform;

-- =====================================================
-- ROLURI
-- =====================================================

CREATE TABLE roles (
    role_id INT AUTO_INCREMENT PRIMARY KEY,
    role_name VARCHAR(50) NOT NULL UNIQUE
);

INSERT INTO roles(role_name)
VALUES ('admin'), ('user');

-- =====================================================
-- UTILIZATORI
-- =====================================================

CREATE TABLE users (
    user_id INT AUTO_INCREMENT PRIMARY KEY,

    role_id INT NOT NULL DEFAULT 2,

    username VARCHAR(50) NOT NULL UNIQUE,
    email VARCHAR(100) NOT NULL UNIQUE,

    password_hash VARCHAR(255) NOT NULL,

    profile_image VARCHAR(255),
    bio TEXT,

    is_online BOOLEAN DEFAULT FALSE,

    last_seen DATETIME,

    theme_preference VARCHAR(20) DEFAULT 'light',

    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT fk_users_roles
        FOREIGN KEY (role_id)
        REFERENCES roles(role_id)
        ON DELETE RESTRICT
        ON UPDATE CASCADE
);

-- =====================================================
-- PRIETENI
-- =====================================================

CREATE TABLE friends (
    friendship_id INT AUTO_INCREMENT PRIMARY KEY,

    user_id INT NOT NULL,
    friend_id INT NOT NULL,

    status ENUM('pending', 'accepted', 'blocked')
        DEFAULT 'pending',

    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT fk_friends_user
        FOREIGN KEY (user_id)
        REFERENCES users(user_id)
        ON DELETE CASCADE
        ON UPDATE CASCADE,

    CONSTRAINT fk_friends_friend
        FOREIGN KEY (friend_id)
        REFERENCES users(user_id)
        ON DELETE CASCADE
        ON UPDATE CASCADE,

    CONSTRAINT uq_friendship
        UNIQUE(user_id, friend_id)
);

CREATE INDEX idx_friends_user_status
ON friends(user_id, status);

CREATE INDEX idx_friends_friend_status
ON friends(friend_id, status);

-- =====================================================
-- CATEGORII JOCURI
-- exemplu:
-- Action, RPG, Survival, Horror
-- =====================================================

CREATE TABLE game_tags (
    tag_id INT AUTO_INCREMENT PRIMARY KEY,

    tag_name VARCHAR(100) NOT NULL UNIQUE
);

-- =====================================================
-- JOCURI
-- =====================================================

CREATE TABLE games (
    game_id INT AUTO_INCREMENT PRIMARY KEY,

    created_by INT NOT NULL,

    title VARCHAR(150) NOT NULL UNIQUE,
    slug VARCHAR(180) NOT NULL UNIQUE,

    short_description TEXT,
    full_description LONGTEXT,

    cover_image VARCHAR(255),
    banner_image VARCHAR(255),

    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT fk_games_users
        FOREIGN KEY (created_by)
        REFERENCES users(user_id)
        ON DELETE RESTRICT
        ON UPDATE CASCADE
);

-- =====================================================
-- RELATIE JOCURI - TAGURI
-- un joc poate avea:
-- Action + RPG + Survival
-- =====================================================

CREATE TABLE game_tag_relations (
    game_id INT NOT NULL,
    tag_id INT NOT NULL,

    PRIMARY KEY(game_id, tag_id),

    CONSTRAINT fk_game_tags_game
        FOREIGN KEY (game_id)
        REFERENCES games(game_id)
        ON DELETE CASCADE
        ON UPDATE CASCADE,

    CONSTRAINT fk_game_tags_tag
        FOREIGN KEY (tag_id)
        REFERENCES game_tags(tag_id)
        ON DELETE CASCADE
        ON UPDATE CASCADE
);

-- =====================================================
-- CATEGORII WIKI
-- exemplu:
-- Bosses, Weapons, NPCs
-- =====================================================

CREATE TABLE categories (
    category_id INT AUTO_INCREMENT PRIMARY KEY,

    game_id INT NOT NULL,

    category_name VARCHAR(100) NOT NULL,
    description TEXT,

    CONSTRAINT fk_categories_games
        FOREIGN KEY (game_id)
        REFERENCES games(game_id)
        ON DELETE CASCADE
        ON UPDATE CASCADE,

    CONSTRAINT uq_game_category
        UNIQUE(game_id, category_name)
);

-- =====================================================
-- ARTICOLE WIKI
-- userii pot crea/modifica articole
-- =====================================================

CREATE TABLE wiki_articles (
    article_id INT AUTO_INCREMENT PRIMARY KEY,

    game_id INT NOT NULL,

    author_id INT NOT NULL,

    title VARCHAR(200) NOT NULL,
    slug VARCHAR(220) NOT NULL UNIQUE,

    summary TEXT,
    content LONGTEXT NOT NULL,

    cover_image VARCHAR(255),

    views_count INT DEFAULT 0,

    is_published BOOLEAN DEFAULT TRUE,

    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,

    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
        ON UPDATE CURRENT_TIMESTAMP,

    CONSTRAINT fk_articles_games
        FOREIGN KEY (game_id)
        REFERENCES games(game_id)
        ON DELETE CASCADE
        ON UPDATE CASCADE,

    CONSTRAINT fk_articles_users
        FOREIGN KEY (author_id)
        REFERENCES users(user_id)
        ON DELETE RESTRICT
        ON UPDATE CASCADE
);

-- =====================================================
-- RELATIE ARTICOLE - CATEGORII
-- =====================================================

CREATE TABLE article_categories (
    article_id INT NOT NULL,
    category_id INT NOT NULL,

    PRIMARY KEY(article_id, category_id),

    CONSTRAINT fk_article_categories_article
        FOREIGN KEY (article_id)
        REFERENCES wiki_articles(article_id)
        ON DELETE CASCADE
        ON UPDATE CASCADE,

    CONSTRAINT fk_article_categories_category
        FOREIGN KEY (category_id)
        REFERENCES categories(category_id)
        ON DELETE CASCADE
        ON UPDATE CASCADE
);

-- =====================================================
-- LINKURI INTRE ARTICOLE
-- exemplu:
-- Bosses -> Moon Lord
-- =====================================================

CREATE TABLE article_links (
    link_id INT AUTO_INCREMENT PRIMARY KEY,

    from_article_id INT NOT NULL,
    to_article_id INT NOT NULL,

    link_text VARCHAR(255),

    CONSTRAINT fk_article_links_from
        FOREIGN KEY (from_article_id)
        REFERENCES wiki_articles(article_id)
        ON DELETE CASCADE
        ON UPDATE CASCADE,

    CONSTRAINT fk_article_links_to
        FOREIGN KEY (to_article_id)
        REFERENCES wiki_articles(article_id)
        ON DELETE CASCADE
        ON UPDATE CASCADE
);

-- =====================================================
-- ISTORIC MODIFICARI ARTICOLE
-- userii pot edita articole
-- =====================================================

CREATE TABLE article_revisions (
    revision_id INT AUTO_INCREMENT PRIMARY KEY,

    article_id INT NOT NULL,

    edited_by INT NOT NULL,

    old_content LONGTEXT NOT NULL,

    edited_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT fk_revision_article
        FOREIGN KEY (article_id)
        REFERENCES wiki_articles(article_id)
        ON DELETE CASCADE
        ON UPDATE CASCADE,

    CONSTRAINT fk_revision_user
        FOREIGN KEY (edited_by)
        REFERENCES users(user_id)
        ON DELETE CASCADE
        ON UPDATE CASCADE
);

-- =====================================================
-- IMAGINI ARTICOLE
-- =====================================================

CREATE TABLE article_images (
    image_id INT AUTO_INCREMENT PRIMARY KEY,

    article_id INT NOT NULL,
    uploaded_by INT NOT NULL,

    image_url VARCHAR(255) NOT NULL,
    alt_text VARCHAR(255),

    CONSTRAINT fk_article_images_articles
        FOREIGN KEY (article_id)
        REFERENCES wiki_articles(article_id)
        ON DELETE CASCADE
        ON UPDATE CASCADE,

    CONSTRAINT fk_article_images_users
        FOREIGN KEY (uploaded_by)
        REFERENCES users(user_id)
        ON DELETE CASCADE
        ON UPDATE CASCADE
);

-- =====================================================
-- COMENTARII
-- =====================================================

CREATE TABLE article_comments (
    comment_id INT AUTO_INCREMENT PRIMARY KEY,

    article_id INT NOT NULL,
    user_id INT NOT NULL,

    comment_text TEXT NOT NULL,

    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT fk_comments_articles
        FOREIGN KEY (article_id)
        REFERENCES wiki_articles(article_id)
        ON DELETE CASCADE
        ON UPDATE CASCADE,

    CONSTRAINT fk_comments_users
        FOREIGN KEY (user_id)
        REFERENCES users(user_id)
        ON DELETE CASCADE
        ON UPDATE CASCADE
);

-- =====================================================
-- LIKE ARTICOLE
-- =====================================================

CREATE TABLE article_likes (
    like_id INT AUTO_INCREMENT PRIMARY KEY,

    article_id INT NOT NULL,
    user_id INT NOT NULL,

    CONSTRAINT fk_likes_article
        FOREIGN KEY (article_id)
        REFERENCES wiki_articles(article_id)
        ON DELETE CASCADE
        ON UPDATE CASCADE,

    CONSTRAINT fk_likes_user
        FOREIGN KEY (user_id)
        REFERENCES users(user_id)
        ON DELETE CASCADE
        ON UPDATE CASCADE,

    CONSTRAINT uq_article_user_like
        UNIQUE(article_id, user_id)
);

-- =====================================================
-- VIZUALIZARI PAGINI
-- o pagina/articol se contorizeaza o singura data per user
-- =====================================================

CREATE TABLE page_views (
    view_id INT AUTO_INCREMENT PRIMARY KEY,

    page_type VARCHAR(20) NOT NULL,
    page_id INT NOT NULL,
    user_id INT NOT NULL,

    viewed_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT fk_page_views_users
        FOREIGN KEY (user_id)
        REFERENCES users(user_id)
        ON DELETE CASCADE
        ON UPDATE CASCADE,

    CONSTRAINT uq_page_view
        UNIQUE(page_type, page_id, user_id)
);

CREATE INDEX idx_page_views_page
ON page_views(page_type, page_id, viewed_at);

CREATE INDEX idx_page_views_user
ON page_views(user_id, page_type, viewed_at);

-- =====================================================
-- ARTICOLE SALVATE
-- =====================================================

CREATE TABLE saved_articles (
    saved_id INT AUTO_INCREMENT PRIMARY KEY,

    user_id INT NOT NULL,
    article_id INT NOT NULL,

    CONSTRAINT fk_saved_user
        FOREIGN KEY (user_id)
        REFERENCES users(user_id)
        ON DELETE CASCADE
        ON UPDATE CASCADE,

    CONSTRAINT fk_saved_article
        FOREIGN KEY (article_id)
        REFERENCES wiki_articles(article_id)
        ON DELETE CASCADE
        ON UPDATE CASCADE,

    CONSTRAINT uq_saved_article
        UNIQUE(user_id, article_id)
);

-- =====================================================
-- TAGURI ARTICOLE
-- =====================================================

CREATE TABLE tags (
    tag_id INT AUTO_INCREMENT PRIMARY KEY,

    tag_name VARCHAR(100) NOT NULL UNIQUE
);

-- =====================================================
-- RELATIE ARTICOLE - TAGURI
-- =====================================================

CREATE TABLE article_tags (
    article_id INT NOT NULL,
    tag_id INT NOT NULL,

    PRIMARY KEY(article_id, tag_id),

    CONSTRAINT fk_article_tags_articles
        FOREIGN KEY (article_id)
        REFERENCES wiki_articles(article_id)
        ON DELETE CASCADE
        ON UPDATE CASCADE,

    CONSTRAINT fk_article_tags_tags
        FOREIGN KEY (tag_id)
        REFERENCES tags(tag_id)
        ON DELETE CASCADE
        ON UPDATE CASCADE
);

-- =====================================================
-- CHAT - CONVERSATII
-- =====================================================

CREATE TABLE conversations (
    conversation_id INT AUTO_INCREMENT PRIMARY KEY,

    created_by INT NOT NULL,

    conversation_name VARCHAR(150),

    is_group_chat BOOLEAN DEFAULT FALSE,

    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT fk_conversations_users
        FOREIGN KEY (created_by)
        REFERENCES users(user_id)
        ON DELETE CASCADE
        ON UPDATE CASCADE
);

-- =====================================================
-- PARTICIPANTI CHAT
-- =====================================================

CREATE TABLE conversation_participants (
    participant_id INT AUTO_INCREMENT PRIMARY KEY,

    conversation_id INT NOT NULL,
    user_id INT NOT NULL,

    CONSTRAINT fk_participants_conversations
        FOREIGN KEY (conversation_id)
        REFERENCES conversations(conversation_id)
        ON DELETE CASCADE
        ON UPDATE CASCADE,

    CONSTRAINT fk_participants_users
        FOREIGN KEY (user_id)
        REFERENCES users(user_id)
        ON DELETE CASCADE
        ON UPDATE CASCADE,

    CONSTRAINT uq_conversation_user
        UNIQUE(conversation_id, user_id)
);

CREATE INDEX idx_participants_user_conversation
ON conversation_participants(user_id, conversation_id);

-- =====================================================
-- MESAJE
-- =====================================================

CREATE TABLE messages (
    message_id INT AUTO_INCREMENT PRIMARY KEY,

    conversation_id INT NOT NULL,
    sender_id INT NOT NULL,

    message_text TEXT,

    image_url VARCHAR(255),

    is_edited BOOLEAN DEFAULT FALSE,
    is_deleted BOOLEAN DEFAULT FALSE,

    sent_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT fk_messages_conversations
        FOREIGN KEY (conversation_id)
        REFERENCES conversations(conversation_id)
        ON DELETE CASCADE
        ON UPDATE CASCADE,

    CONSTRAINT fk_messages_users
        FOREIGN KEY (sender_id)
        REFERENCES users(user_id)
        ON DELETE CASCADE
        ON UPDATE CASCADE
);

CREATE INDEX idx_messages_conversation_sent
ON messages(conversation_id, sent_at, message_id);

-- =====================================================
-- MESAJE CITITE
-- =====================================================

CREATE TABLE message_reads (
    read_id INT AUTO_INCREMENT PRIMARY KEY,

    message_id INT NOT NULL,
    user_id INT NOT NULL,

    read_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT fk_message_reads_messages
        FOREIGN KEY (message_id)
        REFERENCES messages(message_id)
        ON DELETE CASCADE
        ON UPDATE CASCADE,

    CONSTRAINT fk_message_reads_users
        FOREIGN KEY (user_id)
        REFERENCES users(user_id)
        ON DELETE CASCADE
        ON UPDATE CASCADE,

    CONSTRAINT uq_message_user
        UNIQUE(message_id, user_id)
);

CREATE INDEX idx_message_reads_user
ON message_reads(user_id, message_id);

-- =====================================================
-- SESIUNI LOGIN
-- =====================================================

CREATE TABLE user_sessions (
    session_id INT AUTO_INCREMENT PRIMARY KEY,

    user_id INT NOT NULL,

    token VARCHAR(255) NOT NULL UNIQUE,

    ip_address VARCHAR(100),
    user_agent TEXT,

    expires_at DATETIME NOT NULL,

    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT fk_sessions_users
        FOREIGN KEY (user_id)
        REFERENCES users(user_id)
        ON DELETE CASCADE
        ON UPDATE CASCADE
);

-- =====================================================
-- NOTIFICARI
-- =====================================================

CREATE TABLE notifications (
    notification_id INT AUTO_INCREMENT PRIMARY KEY,

    user_id INT NOT NULL,

    source_user_id INT NULL,

    notification_type VARCHAR(50) NOT NULL DEFAULT 'general',

    title VARCHAR(150) NOT NULL,
    message TEXT NOT NULL,

    target_type VARCHAR(50),
    target_id INT,
    action_route VARCHAR(255),

    is_read BOOLEAN DEFAULT FALSE,

    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT fk_notifications_users
        FOREIGN KEY (user_id)
        REFERENCES users(user_id)
        ON DELETE CASCADE
        ON UPDATE CASCADE,
    CONSTRAINT fk_notifications_source_user
        FOREIGN KEY (source_user_id)
        REFERENCES users(user_id)
        ON DELETE SET NULL
        ON UPDATE CASCADE
);

-- =====================================================
-- SECTIUNI HOMEPAGE
-- =====================================================

CREATE TABLE homepage_sections (
    section_id INT AUTO_INCREMENT PRIMARY KEY,

    title VARCHAR(150),
    content TEXT
);

-- =====================================================
-- INDEXURI
-- =====================================================

CREATE INDEX idx_users_username
ON users(username);

CREATE INDEX idx_users_email
ON users(email);

CREATE INDEX idx_games_title
ON games(title);

CREATE INDEX idx_articles_title
ON wiki_articles(title);

CREATE INDEX idx_articles_game
ON wiki_articles(game_id);

CREATE INDEX idx_messages_conversation
ON messages(conversation_id);

CREATE INDEX idx_messages_sender
ON messages(sender_id);

CREATE INDEX idx_comments_article
ON article_comments(article_id);

CREATE INDEX idx_notifications_user
ON notifications(user_id);

CREATE INDEX idx_notifications_user_read_created
ON notifications(user_id, is_read, created_at);

-- =====================================================
-- FULLTEXT SEARCH
-- =====================================================

ALTER TABLE wiki_articles
ADD FULLTEXT(title, summary, content);
