using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mule.File;
using Kademlia;
using Mule.Definitions;

namespace Mule.Core.Impl
{
    class MuleFileTask : MuleTask
    {
        private AbstractFile abstractFile_ = null;
        private KadEntryList kadEntryNotes_ = new KadEntryList();

        public AbstractFile AbstractFile
        {
            get { return abstractFile_; }
            set { abstractFile_ = value; }
        }

        public KadEntryList KadNotes
        {
            get
            {
                return kadEntryNotes_;
            }
        }

        public void LoadComment()
        {
            abstractFile_.FileComment = 
                MuleEngine.CoreObjectManager.Preference.GetFileComment(abstractFile_.FileHash);

            abstractFile_.FileRating = 
                MuleEngine.CoreObjectManager.Preference.GetFileRating(abstractFile_.FileHash);
        }

        public bool AddNote(Kademlia.KadEntry pEntry)
        {
            foreach (Kademlia.KadEntry entry in kadEntryNotes_)
            {
                if (entry.SourceID.Equals(pEntry.SourceID))
                {
                    return false;
                }
            }

            kadEntryNotes_.Insert(0, pEntry);

            UpdateFileRatingCommentAvail();

            return true;
        }

        public virtual void UpdateFileRatingCommentAvail()
        {
            UpdateFileRatingCommentAvail(false);
        }

        public void RefilterKadNotes()
        {
            RefilterKadNotes(true);
        }

        public virtual void RefilterKadNotes(bool bUpdate)
        {
            // check all availabe comments against our filter again
            if (string.IsNullOrEmpty(MuleEngine.CoreObjectManager.Preference.CommentFilter))
            {
                return;
            }

            KadEntryList removed = new KadEntryList();

            string[] filters =
                MuleEngine.CoreObjectManager.Preference.CommentFilter.Split('|');

            if (filters == null || filters.Length == 0)
                return;

            foreach (KadEntry entry in kadEntryNotes_)
            {
                string desc =
                    entry.GetStrTagValue(MuleConstants.TAG_DESCRIPTION);

                if (!string.IsNullOrEmpty(desc))
                {
                    string strCommentLower = desc.ToLower();

                    foreach (string filter in filters)
                    {
                        if (strCommentLower.IndexOf(filter) >= 0)
                        {
                            removed.Add(entry);
                            break;
                        }
                    }
                }
            }

            foreach (KadEntry entry in removed)
                kadEntryNotes_.Remove(entry);

            // untill updated rating and m_bHasComment might be wrong
            if (bUpdate)
            {
                UpdateFileRatingCommentAvail();
            }
        }

        bool IsKadCommentSearchRunning { get; set; }

        public virtual void UpdateFileRatingCommentAvail(bool bForceUpdate)
        {
            bool bOldHasComment = abstractFile_.HasComment;
            uint uOldUserRatings = abstractFile_.UserRating;

            abstractFile_.HasComment = false;
            uint uRatings = 0;
            uint uUserRatings = 0;

            foreach (KadEntry entry in kadEntryNotes_)
            {
                string desc = entry.GetStrTagValue(MuleConstants.TAG_DESCRIPTION);

                if (!abstractFile_.HasComment && !string.IsNullOrEmpty(desc))
                    abstractFile_.HasComment = true;
                uint rating = Convert.ToUInt32(entry.GetIntTagValue(MuleConstants.TAG_FILERATING));

                if (rating != 0)
                {
                    uRatings++;
                    uUserRatings += rating;
                }
            }

            if (uRatings > 0)
                abstractFile_.UserRating = Convert.ToUInt32(Math.Round((float)uUserRatings / (float)uRatings));
            else
                abstractFile_.UserRating = 0;

            if (bOldHasComment != abstractFile_.HasComment ||
                uOldUserRatings != abstractFile_.UserRating ||
                bForceUpdate)
            {
                //TODO: File Event which File comments / user rating changes
            }
        }

    }
}
