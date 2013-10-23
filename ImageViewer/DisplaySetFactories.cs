#region License

// Copyright (c) 2013, ClearCanvas Inc.
// All rights reserved.
// http://www.clearcanvas.ca
//
// This file is part of the ClearCanvas RIS/PACS open source project.
//
// The ClearCanvas RIS/PACS open source project is free software: you can
// redistribute it and/or modify it under the terms of the GNU General Public
// License as published by the Free Software Foundation, either version 3 of the
// License, or (at your option) any later version.
//
// The ClearCanvas RIS/PACS open source project is distributed in the hope that it
// will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU General
// Public License for more details.
//
// You should have received a copy of the GNU General Public License along with
// the ClearCanvas RIS/PACS open source project.  If not, see
// <http://www.gnu.org/licenses/>.

#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using ClearCanvas.Common;
using ClearCanvas.Common.Utilities;
using ClearCanvas.Dicom;
using ClearCanvas.Dicom.ServiceModel.Query;
using ClearCanvas.ImageViewer.Annotations;
using ClearCanvas.ImageViewer.Graphics;
using ClearCanvas.ImageViewer.StudyManagement;

namespace ClearCanvas.ImageViewer
{

	#region Default

	[Cloneable(false)]
	public class SeriesDisplaySetDescriptor : DicomDisplaySetDescriptor
	{
		public SeriesDisplaySetDescriptor(ISeriesIdentifier sourceSeries, IPresentationImageFactory presentationImageFactory)
			: base(sourceSeries, presentationImageFactory)
		{
			Platform.CheckForNullReference(sourceSeries, "sourceSeries");
			Platform.CheckForNullReference(presentationImageFactory, "presentationImageFactory");
		}

		protected SeriesDisplaySetDescriptor(SeriesDisplaySetDescriptor source, ICloningContext context)
			: base(source, context) {}

		protected override string GetName()
		{
			return String.Format("{0}: {1}", SourceSeries.SeriesNumber, SourceSeries.SeriesDescription);
		}

		protected override string GetDescription()
		{
			return SourceSeries.SeriesDescription;
		}

		protected override string GetUid()
		{
			return SourceSeries.SeriesInstanceUid;
		}
	}

	[Cloneable(false)]
	public class SingleFrameDisplaySetDescriptor : DicomDisplaySetDescriptor
	{
		private readonly string _suffix;
		private readonly string _seriesInstanceUid;
		private readonly string _sopInstanceUid;
		private readonly int _frameNumber;
		private readonly int _position;

		public SingleFrameDisplaySetDescriptor(ISeriesIdentifier sourceSeries, Frame frame, int position)
			: base(sourceSeries)
		{
			Platform.CheckForNullReference(sourceSeries, "sourceSeries");
			Platform.CheckForNullReference(frame, "frame");

			_seriesInstanceUid = frame.SeriesInstanceUid;
			_sopInstanceUid = frame.SopInstanceUid;
			_frameNumber = frame.FrameNumber;
			_position = position;

			if (sourceSeries.SeriesInstanceUid == frame.SeriesInstanceUid)
			{
				_suffix = String.Format(SR.SuffixFormatSingleFrameDisplaySet, frame.ParentImageSop.InstanceNumber, _frameNumber);
			}
			else
			{
				//this is a referenced frame (e.g. key iamge).
				_suffix = String.Format(SR.SuffixFormatSingleReferencedFrameDisplaySet,
				                        frame.ParentImageSop.SeriesNumber, frame.ParentImageSop.InstanceNumber, _frameNumber);
			}
		}

		protected SingleFrameDisplaySetDescriptor(SingleFrameDisplaySetDescriptor source, ICloningContext context)
			: base(source, context)
		{
			context.CloneFields(source, this);
		}

		protected override string GetName()
		{
			if (String.IsNullOrEmpty(SourceSeries.SeriesDescription))
				return String.Format("{0}: {1}", SourceSeries.SeriesNumber, _suffix);
			else
				return String.Format("{0}: {1} - {2}", SourceSeries.SeriesNumber, SourceSeries.SeriesDescription, _suffix);
		}

		protected override string GetDescription()
		{
			if (String.IsNullOrEmpty(SourceSeries.SeriesDescription))
				return _suffix;
			else
				return String.Format("{0} - {1}", SourceSeries.SeriesDescription, _suffix);
		}

		protected override string GetUid()
		{
			return String.Format("{0}:{1}:{2}:{3}:{4}", SourceSeries.SeriesInstanceUid, _seriesInstanceUid, _sopInstanceUid, _frameNumber, _position);
		}
	}

	[Cloneable(false)]
	public class SingleImageDisplaySetDescriptor : DicomDisplaySetDescriptor
	{
		private readonly string _suffix;
		private readonly string _seriesInstanceUid;
		private readonly string _sopInstanceUid;
		private readonly int _position;

		public SingleImageDisplaySetDescriptor(ISeriesIdentifier sourceSeries, ImageSop imageSop, int position)
			: base(sourceSeries)
		{
			Platform.CheckForNullReference(sourceSeries, "sourceSeries");
			Platform.CheckForNullReference(imageSop, "imageSop");

			var frame = imageSop.Frames.First();
			_sopInstanceUid = imageSop.SopInstanceUid;
			_seriesInstanceUid = imageSop.SeriesInstanceUid;
			_position = position;

			string laterality = frame.Laterality;
			string viewPosition = frame.ViewPosition;
			if (string.IsNullOrEmpty(viewPosition))
			{
				DicomAttributeSQ codeSequence = frame[DicomTags.ViewCodeSequence] as DicomAttributeSQ;
				if (codeSequence != null && !codeSequence.IsNull && codeSequence.Count > 0)
					viewPosition = codeSequence[0][DicomTags.CodeMeaning].GetString(0, null);
			}

			string lateralityViewPosition = null;
			if (!String.IsNullOrEmpty(laterality) && !String.IsNullOrEmpty(viewPosition))
				lateralityViewPosition = String.Format("{0}/{1}", laterality, viewPosition);
			else if (!String.IsNullOrEmpty(laterality))
				lateralityViewPosition = laterality;
			else if (!String.IsNullOrEmpty(viewPosition))
				lateralityViewPosition = viewPosition;

			if (sourceSeries.SeriesInstanceUid == imageSop.SeriesInstanceUid)
			{
				if (lateralityViewPosition != null)
					_suffix = String.Format(SR.SuffixFormatSingleImageDisplaySetWithLateralityViewPosition, lateralityViewPosition, imageSop.InstanceNumber);
				else
					_suffix = String.Format(SR.SuffixFormatSingleImageDisplaySet, imageSop.InstanceNumber);
			}
			else
			{
				//this is a referenced image (e.g. key image).
				if (lateralityViewPosition != null)
					_suffix = String.Format(SR.SuffixFormatSingleReferencedImageDisplaySetWithLateralityViewPosition,
					                        lateralityViewPosition, imageSop.SeriesNumber, imageSop.InstanceNumber);
				else
					_suffix = String.Format(SR.SuffixFormatSingleReferencedImageDisplaySet,
					                        imageSop.SeriesNumber, imageSop.InstanceNumber);
			}
		}

		protected SingleImageDisplaySetDescriptor(SingleImageDisplaySetDescriptor source, ICloningContext context)
			: base(source, context)
		{
			context.CloneFields(source, this);
		}

		protected override string GetName()
		{
			if (String.IsNullOrEmpty(SourceSeries.SeriesDescription))
				return String.Format("{0}: {1}", SourceSeries.SeriesNumber, _suffix);
			else
				return String.Format("{0}: {1} - {2}", SourceSeries.SeriesNumber, SourceSeries.SeriesDescription, _suffix);
		}

		protected override string GetDescription()
		{
			if (String.IsNullOrEmpty(SourceSeries.SeriesDescription))
				return _suffix;
			else
				return String.Format("{0} - {1}", SourceSeries.SeriesDescription, _suffix);
		}

		protected override string GetUid()
		{
			return String.Format("{0}:{1}:{2}:{3}", SourceSeries.SeriesInstanceUid, _seriesInstanceUid, _sopInstanceUid, _position);
		}
	}

	/// <summary>
	/// A <see cref="DisplaySetFactory"/> for the most typical cases; creating a <see cref="IDisplaySet"/> that
	/// contains <see cref="IPresentationImage"/>s for the entire series, and creating a single <see cref="IDisplaySet"/> for
	/// each image in the series.
	/// </summary>
	public class BasicDisplaySetFactory : DisplaySetFactory
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		public BasicDisplaySetFactory() {}

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="presentationImageFactory">The <see cref="IPresentationImageFactory"/>
		/// used to create the <see cref="IPresentationImage"/>s that populate the constructed <see cref="IDisplaySet"/>s.</param>
		public BasicDisplaySetFactory(IPresentationImageFactory presentationImageFactory)
			: base(presentationImageFactory) {}

		/// <summary>
		/// Specifies whether single image display sets should be created.
		/// </summary>
		/// <remarks>
		/// When this is false, series display sets are created. However, in the degenerate case
		/// where a series has only one image, the factory will not return a display set when
		/// this property is true. Instead, you must set this property to false in order
		/// to get a series display set returned.
		/// </remarks>
		public bool CreateSingleImageDisplaySets { get; set; }

		/// <summary>
		/// Creates <see cref="IDisplaySet"/>s from the given <see cref="Series"/>.
		/// </summary>
		/// <param name="series">The series for which <see cref="IDisplaySet"/>s are to be created.</param>
		/// <returns>A list of created <see cref="IDisplaySet"/>s.</returns>
		public override List<IDisplaySet> CreateDisplaySets(Series series)
		{
			if (CreateSingleImageDisplaySets)
				return DoCreateSingleImageDisplaySets(series);

			var displaySets = new List<IDisplaySet>();
			var displaySet = CreateSeriesDisplaySet(series);
			if (displaySet != null)
			{
				displaySet.PresentationImages.Sort();
				displaySets.Add(displaySet);
			}

			return displaySets;
		}

		private IDisplaySet CreateSeriesDisplaySet(Series series)
		{
			IDisplaySet displaySet = null;
			List<IPresentationImage> images = new List<IPresentationImage>();
			foreach (Sop sop in series.Sops)
				images.AddRange(PresentationImageFactory.CreateImages(sop));

			if (images.Count > 0)
			{
				DisplaySetDescriptor descriptor = new SeriesDisplaySetDescriptor(series.GetIdentifier(), PresentationImageFactory);
				displaySet = new DisplaySet(descriptor);
				foreach (IPresentationImage image in images)
					displaySet.PresentationImages.Add(image);
			}

			return displaySet;
		}

		private List<IDisplaySet> DoCreateSingleImageDisplaySets(Series series)
		{
			List<IDisplaySet> displaySets = new List<IDisplaySet>();
			int position = 0;

			foreach (Sop sop in series.Sops)
			{
				List<IPresentationImage> images = PresentationImageFactory.CreateImages(sop);
				if (images.Count == 0)
					continue;

				if (sop.IsImage)
				{
					ImageSop imageSop = (ImageSop) sop;
					DicomDisplaySetDescriptor descriptor;

					if (imageSop.NumberOfFrames == 1)
						descriptor = new SingleImageDisplaySetDescriptor(series.GetIdentifier(), imageSop, position++);
					else
						descriptor = new MultiframeDisplaySetDescriptor(series.GetIdentifier(), sop.SopInstanceUid, sop.InstanceNumber);

					DisplaySet displaySet = new DisplaySet(descriptor);
					foreach (IPresentationImage image in images)
						displaySet.PresentationImages.Add(image);

					displaySets.Add(displaySet);
				}
				else
				{
					//The sop is actually a container for other referenced sops, like key images.
					foreach (IPresentationImage image in images)
					{
						DisplaySetDescriptor descriptor = null;
						if (image is IImageSopProvider)
						{
							IImageSopProvider provider = (IImageSopProvider) image;
							if (provider.ImageSop.NumberOfFrames == 1)
								descriptor = new SingleImageDisplaySetDescriptor(series.GetIdentifier(), provider.ImageSop, position++);
							else
								descriptor = new SingleFrameDisplaySetDescriptor(series.GetIdentifier(), provider.Frame, position++);
						}
						else
						{
							//TODO (CR Jan 2010): this because the design here is funny... the factory here should actually know something about the key object series it is building for
							ISeriesIdentifier sourceSeries = series.GetIdentifier();
							descriptor = new BasicDisplaySetDescriptor();
							descriptor.Description = sourceSeries.SeriesDescription;
							descriptor.Name = string.Format("{0}: {1}", sourceSeries.SeriesNumber, sourceSeries.SeriesDescription);
							descriptor.Number = sourceSeries.SeriesNumber.GetValueOrDefault(0);
							descriptor.Uid = sourceSeries.SeriesInstanceUid;
						}

						DisplaySet displaySet = new DisplaySet(descriptor);
						displaySet.PresentationImages.Add(image);
						displaySets.Add(displaySet);
					}
				}
			}

			if (displaySets.Count == 1)
			{
				//Degenerate case; single image series, which we're not supposed to create.
				displaySets[0].Dispose();
				displaySets.Clear();
			}

			return displaySets;
		}

		internal static IEnumerable<IDisplaySet> CreateSeriesDisplaySets(Series series, StudyTree studyTree)
		{
			BasicDisplaySetFactory factory = new BasicDisplaySetFactory();
			factory.SetStudyTree(studyTree);
			return factory.CreateDisplaySets(series);
		}
	}

	#endregion

	#region MR Echo

	[Cloneable(false)]
	public class MREchoDisplaySetDescriptor : DicomDisplaySetDescriptor
	{
		private readonly string _suffix;

		public MREchoDisplaySetDescriptor(ISeriesIdentifier sourceSeries, int echoNumber, string stackId, IPresentationImageFactory presentationImageFactory)
			: base(sourceSeries, presentationImageFactory)
		{
			Platform.CheckForNullReference(sourceSeries, "sourceSeries");

			EchoNumber = echoNumber;
			StackId = stackId ?? string.Empty;

			_suffix = String.Format(SR.SuffixFormatMREchoDisplaySet, echoNumber);
			if (!string.IsNullOrEmpty(stackId)) _suffix = string.Concat(_suffix, @" ", String.Format(SR.SuffixFormatMultiframeStackDisplaySet, stackId));
		}

		protected MREchoDisplaySetDescriptor(MREchoDisplaySetDescriptor source, ICloningContext context)
			: base(source, context)
		{
			context.CloneFields(source, this);
		}

		public int EchoNumber { get; private set; }
		public string StackId { get; private set; }

		protected override string GetName()
		{
			if (String.IsNullOrEmpty(base.SourceSeries.SeriesDescription))
				return String.Format("{0}: {1}", SourceSeries.SeriesNumber, _suffix);
			else
				return String.Format("{0}: {1} - {2}", SourceSeries.SeriesNumber, SourceSeries.SeriesDescription, _suffix);
		}

		protected override string GetDescription()
		{
			if (String.IsNullOrEmpty(base.SourceSeries.SeriesDescription))
				return _suffix;
			else
				return String.Format("{0} - {1}", SourceSeries.SeriesDescription, _suffix);
		}

		protected override string GetUid()
		{
			if (!string.IsNullOrEmpty(StackId))
				return String.Format("{0}:Echo{1}:Stack-{2}", SourceSeries.SeriesInstanceUid, EchoNumber, StackId);
			else
				return String.Format("{0}:Echo{1}", SourceSeries.SeriesInstanceUid, EchoNumber);
		}
	}

	/// <summary>
	/// A <see cref="DisplaySetFactory"/> that splits MR series with multiple echoes into multiple <see cref="IDisplaySet"/>s; one per echo.
	/// </summary>
	public class MREchoDisplaySetFactory : DisplaySetFactory
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		public MREchoDisplaySetFactory() {}

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="presentationImageFactory">The <see cref="IPresentationImageFactory"/>
		/// used to create the <see cref="IPresentationImage"/>s that populate the constructed <see cref="IDisplaySet"/>s.</param>
		public MREchoDisplaySetFactory(IPresentationImageFactory presentationImageFactory)
			: base(presentationImageFactory) {}

		/// <summary>
		/// Gets or sets whether or not to split by multiframe stack as well as echo.
		/// </summary>
		public bool SplitStacks { get; set; }

		/// <summary>
		/// Creates zero or more <see cref="IDisplaySet"/>s from the given <see cref="Series"/>.
		/// </summary>
		/// <remarks>
		/// When the input <see cref="Series"/> does not have multiple echoes, no <see cref="IDisplaySet"/>s will be returned.
		/// Otherwise, at least 2 <see cref="IDisplaySet"/>s will be returned.
		/// </remarks>
		public override List<IDisplaySet> CreateDisplaySets(Series series)
		{
			List<IDisplaySet> displaySets = new List<IDisplaySet>();

			if (series.Modality == "MR")
			{
				var imagesByEchoNumber = SplitMREchos(series.Sops);

				if (imagesByEchoNumber.Count > 1)
				{
					foreach (var echoImages in imagesByEchoNumber)
					{
						var images = echoImages.Value;
						if (images.Count > 0)
						{
							IDisplaySet displaySet = new DisplaySet(new MREchoDisplaySetDescriptor(series.GetIdentifier(), echoImages.Key.EchoIndexValue, echoImages.Key.StackId, PresentationImageFactory));
							foreach (IPresentationImage image in images)
								displaySet.PresentationImages.Add(image);

							displaySet.PresentationImages.Sort();
							displaySets.Add(displaySet);
						}
					}
				}
			}

			return displaySets;
		}

		private IList<KeyValuePair<EchoIndex, List<IPresentationImage>>> SplitMREchos(IEnumerable<Sop> sops)
		{
			// keep separate echo lists in case series is mixed single/multi frames
			// unless their dimension uid also matches, echo dimension indices should not be mixed between sop intances (see DICOM 2011 PS 3.3 C.7.6.17.2)
			SortedDictionary<int, List<IPresentationImage>> imagesByEchoNumber = new SortedDictionary<int, List<IPresentationImage>>();
			Dictionary<EchoIndex, List<IPresentationImage>> imagesByEchoDimension = new Dictionary<EchoIndex, List<IPresentationImage>>();

			foreach (var sop in sops.OfType<ImageSop>())
			{
				if (sop.IsMultiframe)
				{
					foreach (var image in PresentationImageFactory.CreateImages(sop))
					{
						// key the display sets by echo and stack within the multiframe instance
						var frame = ((IImageSopProvider) image).Frame;
						var echoIndex = SplitStacks ? new EchoIndex(sop.SopInstanceUid, frame.EchoNumber, frame.StackNumber, frame.StackId) : new EchoIndex(sop.SopInstanceUid, frame.EchoNumber);
						if (!imagesByEchoDimension.ContainsKey(echoIndex))
							imagesByEchoDimension[echoIndex] = new List<IPresentationImage>();
						imagesByEchoDimension[echoIndex].Add(image);
					}
				}
				else
				{
					DicomAttribute echoAttribute = sop[DicomTags.EchoNumbers];
					if (!echoAttribute.IsEmpty)
					{
						int echoNumber = echoAttribute.GetInt32(0, 0);
						if (!imagesByEchoNumber.ContainsKey(echoNumber))
							imagesByEchoNumber[echoNumber] = new List<IPresentationImage>();

						imagesByEchoNumber[echoNumber].AddRange(PresentationImageFactory.CreateImages(sop));
					}
				}
			}

			var results = imagesByEchoNumber.Select(k => new KeyValuePair<EchoIndex, List<IPresentationImage>>(new EchoIndex(null, k.Key), k.Value)).ToList();

			// if we have some multiframes processed into separate echos, append them to the main list
			if (imagesByEchoDimension.Count > 0)
			{
				// make sure we number them in a logical order - since the multiframe echo indices don't have any meaning outside the SOP instance in which it was used,
				// we'll sort by the first instance number in which the index was used, then order by the actual index value
				results.AddRange(imagesByEchoDimension
				                 	.OrderBy(k => k.Value.Select(i => ((IImageSopProvider) i).ImageSop.InstanceNumber).Min())
				                 	.ThenBy(k => k.Key.EchoIndexValue)
				                 	.ThenBy(k => k.Key.StackIndexValue.GetValueOrDefault(-1)));
			}

			return results;
		}

		private class EchoIndex : IEquatable<EchoIndex>
		{
			private readonly string _dimensionOrganizationUid;
			private readonly int _echoIndexValue;
			private readonly int? _stackIndexValue;
			private readonly string _stackId;

			public EchoIndex(string dimensionOrganizationUid, int echoIndexValue, int? stackIndexValue = null, string stackId = null)
			{
				_dimensionOrganizationUid = dimensionOrganizationUid ?? string.Empty;
				_echoIndexValue = echoIndexValue;
				_stackIndexValue = stackIndexValue;
				_stackId = stackId ?? string.Empty;
			}

			public int EchoIndexValue
			{
				get { return _echoIndexValue; }
			}

			public int? StackIndexValue
			{
				get { return _stackIndexValue; }
			}

			public string StackId
			{
				get { return _stackId; }
			}

			public override int GetHashCode()
			{
				return _dimensionOrganizationUid.GetHashCode() ^ _echoIndexValue.GetHashCode() ^ _stackId.GetHashCode();
			}

			public bool Equals(EchoIndex other)
			{
				return other != null && Equals(_dimensionOrganizationUid, other._dimensionOrganizationUid) && _echoIndexValue == other._echoIndexValue && Equals(_stackId, other._stackId);
			}

			public override bool Equals(object obj)
			{
				return Equals(obj as EchoIndex);
			}
		}
	}

	#endregion

	#region Multi-frame Stack

	[Cloneable(false)]
	public class MultiFrameStackDisplaySetDescriptor : DicomDisplaySetDescriptor
	{
		private readonly string _suffix;

		public MultiFrameStackDisplaySetDescriptor(ISeriesIdentifier sourceSeries, string stackId, IPresentationImageFactory presentationImageFactory)
			: base(sourceSeries, presentationImageFactory)
		{
			Platform.CheckForNullReference(sourceSeries, "sourceSeries");

			StackId = stackId ?? string.Empty;

			_suffix = String.Format(SR.SuffixFormatMultiframeStackDisplaySet, stackId);
		}

		protected MultiFrameStackDisplaySetDescriptor(MultiFrameStackDisplaySetDescriptor source, ICloningContext context)
			: base(source, context)
		{
			context.CloneFields(source, this);
		}

		public string StackId { get; private set; }

		protected override string GetName()
		{
			if (String.IsNullOrEmpty(SourceSeries.SeriesDescription))
				return String.Format("{0}: {1}", SourceSeries.SeriesNumber, _suffix);
			else
				return String.Format("{0}: {1} - {2}", SourceSeries.SeriesNumber, SourceSeries.SeriesDescription, _suffix);
		}

		protected override string GetDescription()
		{
			if (String.IsNullOrEmpty(SourceSeries.SeriesDescription))
				return _suffix;
			else
				return String.Format("{0} - {1}", SourceSeries.SeriesDescription, _suffix);
		}

		protected override string GetUid()
		{
			return String.Format("{0}:Stack-{1}", SourceSeries.SeriesInstanceUid, StackId);
		}
	}

	/// <summary>
	/// A <see cref="DisplaySetFactory"/> that splits multi-frame instances with multiple stacks into multiple <see cref="IDisplaySet"/>s; one per stack.
	/// </summary>
	public class MultiFrameStackDisplaySetFactory : DisplaySetFactory
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		public MultiFrameStackDisplaySetFactory() {}

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="presentationImageFactory">The <see cref="IPresentationImageFactory"/>
		/// used to create the <see cref="IPresentationImage"/>s that populate the constructed <see cref="IDisplaySet"/>s.</param>
		public MultiFrameStackDisplaySetFactory(IPresentationImageFactory presentationImageFactory)
			: base(presentationImageFactory) {}

		/// <summary>
		/// Creates zero or more <see cref="IDisplaySet"/>s from the given <see cref="Series"/>.
		/// </summary>
		/// <remarks>
		/// When the input <see cref="Series"/> does not have multiple echoes, no <see cref="IDisplaySet"/>s will be returned.
		/// Otherwise, at least 2 <see cref="IDisplaySet"/>s will be returned.
		/// </remarks>
		public override List<IDisplaySet> CreateDisplaySets(Series series)
		{
			List<IDisplaySet> displaySets = new List<IDisplaySet>();

			var imagesByStack = SplitStacks(series.Sops);

			if (imagesByStack.Count > 1)
			{
				foreach (var stackImages in imagesByStack)
				{
					var images = stackImages.Value;
					if (images.Count > 0)
					{
						IDisplaySet displaySet = new DisplaySet(new MultiFrameStackDisplaySetDescriptor(series.GetIdentifier(), stackImages.Key.StackId, PresentationImageFactory));
						foreach (IPresentationImage image in images)
							displaySet.PresentationImages.Add(image);

						displaySet.PresentationImages.Sort();
						displaySets.Add(displaySet);
					}
				}
			}

			return displaySets;
		}

		private IList<KeyValuePair<StackIndex, List<IPresentationImage>>> SplitStacks(IEnumerable<Sop> sops)
		{
			// keep separate echo lists in case series is mixed single/multi frames
			// unless their dimension uid also matches, echo dimension indices should not be mixed between sop intances (see DICOM 2011 PS 3.3 C.7.6.17.2)
			SortedDictionary<int, List<IPresentationImage>> singleFrames = new SortedDictionary<int, List<IPresentationImage>>();
			Dictionary<StackIndex, List<IPresentationImage>> imagesByStackDimension = new Dictionary<StackIndex, List<IPresentationImage>>();

			foreach (var sop in sops.OfType<ImageSop>())
			{
				if (sop.IsMultiframe)
				{
					foreach (var image in PresentationImageFactory.CreateImages(sop))
					{
						// key the display sets by stack within the multiframe instance
						var frame = ((IImageSopProvider) image).Frame;
						var stackIndex = new StackIndex(sop.SopInstanceUid, frame.StackNumber, frame.StackId);
						if (!imagesByStackDimension.ContainsKey(stackIndex))
							imagesByStackDimension[stackIndex] = new List<IPresentationImage>();
						imagesByStackDimension[stackIndex].Add(image);
					}
				}
				else
				{
					// single frame images don't really have a stack, so just assign them all to a stack 0
					const int stackNumber = 0;
					if (!singleFrames.ContainsKey(stackNumber))
						singleFrames[stackNumber] = new List<IPresentationImage>();
					singleFrames[stackNumber].AddRange(PresentationImageFactory.CreateImages(sop));
				}
			}

			var results = singleFrames.Select(k => new KeyValuePair<StackIndex, List<IPresentationImage>>(new StackIndex(null, null, string.Empty), k.Value)).ToList();

			// if we have some multiframes processed into separate echos, append them to the main list
			if (imagesByStackDimension.Count > 0)
			{
				// make sure we number them in a logical order - since the multiframe echo indices don't have any meaning outside the SOP instance in which it was used,
				// we'll sort by the first instance number in which the index was used, then order by the actual index value
				results.AddRange(imagesByStackDimension
				                 	.OrderBy(k => k.Value.Select(i => ((IImageSopProvider) i).ImageSop.InstanceNumber).Min())
				                 	.ThenBy(k => k.Key.StackIndexValue.GetValueOrDefault(-1)));
			}

			return results;
		}

		private class StackIndex : IEquatable<StackIndex>
		{
			private readonly string _dimensionOrganizationUid;
			private readonly int? _stackIndexValue;
			private readonly string _stackId;

			public StackIndex(string dimensionOrganizationUid, int? stackIndexValue, string stackId)
			{
				_dimensionOrganizationUid = dimensionOrganizationUid ?? string.Empty;
				_stackIndexValue = stackIndexValue;
				_stackId = stackId ?? string.Empty;
			}

			public string StackId
			{
				get { return _stackId; }
			}

			public int? StackIndexValue
			{
				get { return _stackIndexValue; }
			}

			public override int GetHashCode()
			{
				return _dimensionOrganizationUid.GetHashCode() ^ _stackId.GetHashCode();
			}

			public bool Equals(StackIndex other)
			{
				return other != null && Equals(_dimensionOrganizationUid, other._dimensionOrganizationUid) && Equals(_stackId, other._stackId);
			}

			public override bool Equals(object obj)
			{
				return Equals(obj as StackIndex);
			}
		}
	}

	#endregion

	#region Mixed Multi-frame

	[Cloneable(false)]
	public class MultiframeDisplaySetDescriptor : DicomDisplaySetDescriptor
	{
		private readonly string _sopInstanceUid;
		private readonly string _suffix;

		public MultiframeDisplaySetDescriptor(ISeriesIdentifier sourceSeries, string sopInstanceUid, int instanceNumber)
			: base(sourceSeries)
		{
			SopInstanceUid = sopInstanceUid;
			InstanceNumber = instanceNumber;
			Platform.CheckForNullReference(sourceSeries, "sourceSeries");
			Platform.CheckForEmptyString(sopInstanceUid, "sopInstanceUid");

			_sopInstanceUid = sopInstanceUid;
			_suffix = String.Format(SR.SuffixFormatMultiframeDisplaySet, instanceNumber);
		}

		protected MultiframeDisplaySetDescriptor(MultiframeDisplaySetDescriptor source, ICloningContext context)
			: base(source, context)
		{
			context.CloneFields(source, this);
		}

		public string SopInstanceUid { get; private set; }
		public int InstanceNumber { get; private set; }

		protected override string GetName()
		{
			if (String.IsNullOrEmpty(base.SourceSeries.SeriesDescription))
				return String.Format("{0}: {1}", SourceSeries.SeriesNumber, _suffix);
			else
				return String.Format("{0}: {1} - {2}", SourceSeries.SeriesNumber, SourceSeries.SeriesDescription, _suffix);
		}

		protected override string GetDescription()
		{
			if (String.IsNullOrEmpty(base.SourceSeries.SeriesDescription))
				return _suffix;
			else
				return String.Format("{0} - {1}", SourceSeries.SeriesDescription, _suffix);
		}

		protected override string GetUid()
		{
			return _sopInstanceUid;
		}
	}

	[Cloneable(false)]
	public class SingleImagesDisplaySetDescriptor : DicomDisplaySetDescriptor
	{
		private readonly string _suffix;

		public SingleImagesDisplaySetDescriptor(ISeriesIdentifier sourceSeries, IPresentationImageFactory presentationImageFactory)
			: base(sourceSeries, presentationImageFactory)
		{
			Platform.CheckForNullReference(sourceSeries, "sourceSeries");

			_suffix = SR.SuffixSingleImagesDisplaySet;
		}

		protected SingleImagesDisplaySetDescriptor(SingleImagesDisplaySetDescriptor source, ICloningContext context)
			: base(source, context)
		{
			context.CloneFields(source, this);
		}

		protected override string GetName()
		{
			if (String.IsNullOrEmpty(base.SourceSeries.SeriesDescription))
				return String.Format("{0}: {1}", SourceSeries.SeriesNumber, _suffix);
			else
				return String.Format("{0}: {1} - {2}", SourceSeries.SeriesNumber, SourceSeries.SeriesDescription, _suffix);
		}

		protected override string GetDescription()
		{
			if (String.IsNullOrEmpty(base.SourceSeries.SeriesDescription))
				return _suffix;
			else
				return String.Format("{0} - {1}", SourceSeries.SeriesDescription, _suffix);
		}

		protected override string GetUid()
		{
			return String.Format("{0}:SingleImages", SourceSeries.SeriesInstanceUid);
		}
	}

	/// <summary>
	/// A <see cref="DisplaySetFactory"/> that splits series with multiple single or multiframe images into
	/// separate <see cref="IDisplaySet"/>s.
	/// </summary>
	/// <remarks>
	/// This factory will only create <see cref="IDisplaySet"/>s when the following is true.
	/// <list type="bullet">
	/// <item>The input series contains more than one multiframe image.</item>
	/// <item>The input series contains at least one multiframe image and at least one single frame image.</item>
	/// </list>
	/// For typical series, consisting only of single frame images, no <see cref="IDisplaySet"/>s will be created.
	/// The <see cref="IDisplaySet"/>s that are created are:
	/// <list type="bullet">
	/// <item>One <see cref="IDisplaySet"/> per multiframe image.</item>
	/// <item>One <see cref="IDisplaySet"/> containing all the single frame images, if any.</item>
	/// </list>
	/// </remarks>
	public class MixedMultiFrameDisplaySetFactory : DisplaySetFactory
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		public MixedMultiFrameDisplaySetFactory() {}

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="presentationImageFactory">The <see cref="IPresentationImageFactory"/>
		/// used to create the <see cref="IPresentationImage"/>s that populate the constructed <see cref="IDisplaySet"/>s.</param>
		public MixedMultiFrameDisplaySetFactory(IPresentationImageFactory presentationImageFactory)
			: base(presentationImageFactory) {}

		/// <summary>
		/// Creates zero or more <see cref="IDisplaySet"/>s from the given <see cref="Series"/>.
		/// </summary>
		/// <remarks>When the input series does not contain a mixture of single and multiframe
		/// images, no <see cref="IDisplaySet"/>s will be returned.</remarks>
		public override List<IDisplaySet> CreateDisplaySets(Series series)
		{
			List<IDisplaySet> displaySets = new List<IDisplaySet>();

			List<ImageSop> singleFrames = new List<ImageSop>();
			List<ImageSop> multiFrames = new List<ImageSop>();

			foreach (Sop sop in series.Sops)
			{
				if (sop.IsImage)
				{
					ImageSop imageSop = (ImageSop) sop;
					if (imageSop.NumberOfFrames > 1)
						multiFrames.Add(imageSop);
					else
						singleFrames.Add(imageSop);
				}
			}

			if (multiFrames.Count > 1 || (singleFrames.Count > 0 && multiFrames.Count > 0))
			{
				if (singleFrames.Count > 0)
				{
					List<IPresentationImage> singleFrameImages = new List<IPresentationImage>();
					foreach (ImageSop singleFrame in singleFrames)
						singleFrameImages.AddRange(PresentationImageFactory.CreateImages(singleFrame));

					if (singleFrameImages.Count > 0)
					{
						var descriptor = new SingleImagesDisplaySetDescriptor(series.GetIdentifier(), PresentationImageFactory);
						var singleImagesDisplaySet = new DisplaySet(descriptor);

						foreach (IPresentationImage singleFrameImage in singleFrameImages)
							singleImagesDisplaySet.PresentationImages.Add(singleFrameImage);

						singleImagesDisplaySet.PresentationImages.Sort();
						displaySets.Add(singleImagesDisplaySet);
					}
				}

				foreach (ImageSop multiFrame in multiFrames)
				{
					List<IPresentationImage> multiFrameImages = PresentationImageFactory.CreateImages(multiFrame);
					if (multiFrameImages.Count > 0)
					{
						MultiframeDisplaySetDescriptor descriptor =
							new MultiframeDisplaySetDescriptor(multiFrame.ParentSeries.GetIdentifier(), multiFrame.SopInstanceUid, multiFrame.InstanceNumber);
						DisplaySet displaySet = new DisplaySet(descriptor);

						foreach (IPresentationImage multiFrameImage in multiFrameImages)
							displaySet.PresentationImages.Add(multiFrameImage);

						displaySet.PresentationImages.Sort();
						displaySets.Add(displaySet);
					}
				}
			}

			return displaySets;
		}
	}

	#endregion

	#region EntireStudy

	[Cloneable(false)]
	public class ModalityDisplaySetDescriptor : DicomDisplaySetDescriptor
	{
		public ModalityDisplaySetDescriptor(IStudyIdentifier sourceStudy, string modality, IPresentationImageFactory presentationImageFactory)
			: base(null, presentationImageFactory)
		{
			Platform.CheckForNullReference(sourceStudy, "sourceStudy");
			Platform.CheckForEmptyString(modality, "modality");

			SourceStudy = sourceStudy;
			Modality = modality;
		}

		protected ModalityDisplaySetDescriptor(ModalityDisplaySetDescriptor source, ICloningContext context)
			: base(source, context)
		{
			//context.CloneFields(source, this);
			Modality = source.Modality;
			SourceStudy = source.SourceStudy;
		}

		public IStudyIdentifier SourceStudy { get; private set; }
		public string Modality { get; private set; }

		protected override string GetName()
		{
			return String.Format(SR.FormatNameModalityDisplaySet, Modality);
		}

		protected override string GetDescription()
		{
			return String.Format(SR.FormatDescriptionModalityDisplaySet, Modality);
		}

		protected override string GetUid()
		{
			return String.Format("AllImages_{0}_{1}", Modality, SourceStudy.StudyInstanceUid);
		}
	}

	public class ModalityDisplaySetFactory : DisplaySetFactory
	{
		public ModalityDisplaySetFactory() {}

		public ModalityDisplaySetFactory(IPresentationImageFactory presentationImageFactory)
			: base(presentationImageFactory) {}

		private IDisplaySet CreateDisplaySet(Study study, IEnumerable<Series> modalitySeries)
		{
			var first = modalitySeries.FirstOrDefault();
			if (first == null)
				return null;

			var modality = first.Modality;
			if (String.IsNullOrEmpty(modality))
				return null;

			var displaySet = new DisplaySet(new ModalityDisplaySetDescriptor(study.GetIdentifier(), modality, PresentationImageFactory));
			int seriesCount = 0;
			foreach (var series in modalitySeries)
			{
				bool added = false;
				foreach (var imageSop in series.Sops) //We don't want key images, references etc.
				{
					foreach (var image in PresentationImageFactory.CreateImages(imageSop))
					{
						displaySet.PresentationImages.Add(image);
						added = true;
					}
				}

				if (added)
					++seriesCount;
			}

			// Degenerate case is one series, in which case we don't create this display set.
			if (seriesCount > 1)
				return displaySet;

			displaySet.Dispose();
			return null;
		}

		public IDisplaySet CreateDisplaySet(Study study, string modality)
		{
			return CreateDisplaySet(study, study.Series.Where(s => s.Modality == modality));
		}

		public override List<IDisplaySet> CreateDisplaySets(Study study)
		{
			var displaySets = new List<IDisplaySet>();
			foreach (var seriesByModality in study.Series.GroupBy(s => s.Modality))
			{
				var displaySet = CreateDisplaySet(study, seriesByModality);
				if (displaySet != null)
				{
					displaySet.PresentationImages.Sort();
					displaySets.Add(displaySet);
				}
			}

			return displaySets;
		}

		public override List<IDisplaySet> CreateDisplaySets(Series series)
		{
			throw new NotSupportedException();
		}
	}

	#endregion

	#region Placeholder

	public class PlaceholderDisplaySetFactory : DisplaySetFactory
	{
		public override List<IDisplaySet> CreateDisplaySets(Series series)
		{
			var images = new List<IPresentationImage>();
			foreach (var sop in series.Sops)
			{
				// TODO CR (Oct 11): To be reworked before next Community release, since we do want this to show

				// only create placeholders for any non-image, non-presentation state SOPs
				if (sop.IsImage
				    || sop.SopClassUid == SopClass.EncapsulatedPdfStorageUid
				    || sop.SopClassUid == SopClass.GrayscaleSoftcopyPresentationStateStorageSopClassUid
				    || sop.SopClassUid == SopClass.ColorSoftcopyPresentationStateStorageSopClassUid
				    || sop.SopClassUid == SopClass.PseudoColorSoftcopyPresentationStateStorageSopClassUid
				    || sop.SopClassUid == SopClass.BlendingSoftcopyPresentationStateStorageSopClassUid)
					continue;

				images.Add(new PlaceholderPresentationImage(sop));
			}

			if (images.Count > 0)
			{
				var displaySet = new DisplaySet(new SeriesDisplaySetDescriptor(series.GetIdentifier(), PresentationImageFactory));
				foreach (var image in images)
					displaySet.PresentationImages.Add(image);

				return new List<IDisplaySet>(new[] {displaySet});
			}

			return new List<IDisplaySet>();
		}

		#region PlaceholderPresentationImage Class

		[Cloneable]
		private sealed class PlaceholderPresentationImage : BasicPresentationImage, ISopProvider
		{
			[CloneIgnore]
			private ISopReference _sopReference;

			public PlaceholderPresentationImage(Sop sop)
				: base(new GrayscaleImageGraphic(1, 1))
			{
				_sopReference = sop.CreateTransientReference();

				var sopClass = SopClass.GetSopClass(sop.SopClassUid);
				var sopClassDescription = sopClass != null ? sopClass.Name : SR.LabelUnknown;
				CompositeImageGraphic.Graphics.Add(new ErrorMessageGraphic {Text = string.Format(SR.MessageUnsupportedImageType, sopClassDescription), Color = Color.WhiteSmoke});
				Platform.Log(LogLevel.Warn, "Unsupported SOP Class \"{0} ({1})\" (SOP Instance {2})", sopClassDescription, sop.SopClassUid, sop.SopInstanceUid);
			}

			/// <summary>
			/// Cloning constructor.
			/// </summary>
			/// <param name="source">The source object from which to clone.</param>
			/// <param name="context">The cloning context object.</param>
			private PlaceholderPresentationImage(PlaceholderPresentationImage source, ICloningContext context)
				: base(source, context)
			{
				_sopReference = source._sopReference.Clone();

				context.CloneFields(source, this);
			}

			protected override void Dispose(bool disposing)
			{
				if (_sopReference != null)
				{
					_sopReference.Dispose();
					_sopReference = null;
				}
				base.Dispose(disposing);
			}

			public Sop Sop
			{
				get { return _sopReference.Sop; }
			}

			protected override IAnnotationLayout CreateAnnotationLayout()
			{
				return new AnnotationLayout();
			}

			public override IPresentationImage CreateFreshCopy()
			{
				return new PlaceholderPresentationImage(_sopReference.Sop);
			}

			[Cloneable(true)]
			private class ErrorMessageGraphic : InvariantTextPrimitive
			{
				protected override SpatialTransform CreateSpatialTransform()
				{
					return new InvariantSpatialTransform(this);
				}

				public override void OnDrawing()
				{
					if (base.ParentPresentationImage != null)
					{
						CoordinateSystem = CoordinateSystem.Destination;
						try
						{
							var clientRectangle = ParentPresentationImage.ClientRectangle;
							Location = new PointF(clientRectangle.Width/2f, clientRectangle.Height/2f);
						}
						finally
						{
							ResetCoordinateSystem();
						}
					}
					base.OnDrawing();
				}
			}
		}

		#endregion
	}

	#endregion
}