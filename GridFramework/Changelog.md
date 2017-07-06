Changelog {#changelog}
=========


Version 2.1.x
-------------

### Version 2.1.4 ###

- Fix incorrect algorithm in extension method `NearestFace` for hex grids
- Downgrade project, minimum Unity version is now 5.0
- Fix missing documentation
- Images in the HTML documentation will no longer be recognised as images by
  Unity and will not spam your texture selector anymore.


### Version 2.1.3 ###

The examples have been improved upon. They are now nicer to look at, and the
code is simpler and cleaner in many cases.


### Version 2.1.2 ###

Fix the Vectrosity example throwing "Index out of range" errors. The following
changes have been made to the `GridRenderer` class:

- The protected field `_lineCount` is no longer being serialised
- The public property `LineSets` is no longer being serialised

None of these changes affect client code, compatibility is unaffected.


### Version 2.1.1 ###
Maintenance update.

- Fix warning about `data_path` of inspectors in Unity 5.4.


### Version 2.1.0 ###
Make some computed properties writable. These properties depend on only one of
the main properties of the grid and setting a computed property implicitly sets
the main property. The properties are:

- In hexagonal grids: `Width`, `Height`, `Side`
- In polar grids: `Radians`, `Degrees`
- In spherical grids: `Polar`, `PolarDeg`, `Azimuth`, `AzimuthDeg`

These properties also show up in the inspectors behind a foldout to toggle
their display.

Fix compiler errors when using Playmaker actions. The errors were due to
referencing a property that does not exist anymore and due to a name collision
with one of Playmaker's own classes.

Remove an instance where it was theoretically possible for internal code to
access an unassigned variable. This issue could never happen in practice, but
the Visual Studio compiler was complaining regardless.


Version 2.0.x
-------------

### Version 2.0.1 ###
Fix a typo that prevented the editor of polar grids from showing up. Fixed
custom inspector for the `Cylinder` renderer of polar grids not showing.


### Version 2.0.0 ###
This is a major release which breaks compatibility with the version 1.x
releases. Please do not upgrade if you are not willing to refactor your code.

Version 2 brings large improvements in terms of simplicity and extensibility
for Grid Framework. The API has been massively cut down in complexity without
losing any important features, large classes have been split up into smaller
specialised classes that work together and the rendering system has been
reworked to be more reliable and more flexible. You can define your own grid
and renderers and take control over the rendering process as you wish.

The biggest change is the introduction of *renderers*, new components that take
the task of putting grids on the screen. There is a separate renderer for every
shape of grid. This has allowed me to move large amounts of code from the grid
classes into the respecting renderer classes, making the API much leaner in the
process.

Another big change which has help streamline the API is the introduction of
official extension methods. Functionality which does not strictly belong into a
grid but is still very useful to have has now been moved into specialised
extension methods. Once you import their namespace you won't be able to tell
them apart from original methods, but until then they won't clutter up your
API.

All classes are now residing in custom namespaces. This avoids the extra noise
created by the `GF` prefix everywhere. All namespaces are part of the
`GridFramework` namespace, which isolates grids, renderers, extensions, editors
and even examples from the rest of your codebase.

A completely new rendering system offers ways to hook into the process yourself
and take control if you are not satisfied by the defaults.

There are many more changes under the hood, too many to mention in this
changelog, so check out the upgrade guide of the user manual for more details.

-------------------------------------------------------------------------------

Version 1.9.x
-------------

###Version 1.9.0###
- *New:* A new grid type, spherical grids.
- *Manual:* Chapter with frequently used code snippets.


Version 1.8.x
-------------

###Version 1.8.4###

- *Changed:* Support for Vectrosity and Playmaker needs to be enabled
  explicitly now. Please consult the user manual chapter @ref plugins.

- *Fixed:* Updated Vectrosity support and examples to version 5.

- *New:* A new example showcases an infinited grid in 3D.



###Version 1.8.3###

- *New:* The lower limit of various grid properties is now `Mathf.Epsilon`
  instead of `0.1`.

- *Change:* Unity version 5 requires now a shader file to render grids. In
  previous versions the default shader was compiled at runtime from a string,
  but Unity 5 has deprecated this practice.



###Version 1.8.2###

- *New:* Auto-detect Playmaker for Unity 4 and Unity 5.



###Version 1.8.1###

- *Fixed:* Compilation error in Playmaker action



###Version 1.8.0###

Introducing a new rendering shape for hex grids.

- *New:* Hex grids can render in a circular shape.

- *New:* `renderAround` property on hex grids for the new shape.

- *Fixed:* Size and rendering range not showing up properly in the inspector.



Version 1.7.x
-------------

###Version 1.7.4###

This release brings seamless compatibility with Unity 5. The changes are all
under the hood and don't affect anything about Grid Frameworks API or its
results.



###Version 1.7.3###

The following changes affect the grid inspectors, but *not* the API.

- `relativeSize` is now `true` by default now.

- `useCustomRenderRange` is `true` by default now.

- `renderFrom` is `(-5, -5, -5)` by default now.

- `renderFrom` is `(5, 5, 5)` by default now.

- The *rendering range* now take the place of the *size* in the inspector. The
  *size* only shows up when the *Use Custom Render Range* flag is unchecked.


Further changes:

- Fixed missing reference in Snapping Units example

- Fixed too fast camera speen in Endless Grid example



###Version 1.7.2###

- *Fixed:* Null exception on polar grids when getting Vectrosity points if the
  grid is not being rendered.



###Version 1.7.1###

- *Fixed:* The grid align panel now correctly respect or ignores rotation when
auto-snapping.



###Version 1.7.0###

This release features a number of new coordinate systems and corresponding
rendering shapes.

- *New:* Downwards herringbone coordinate system for hex grids

- *New:* Downwards rectangle rendering shape to accompany the new coordinate
  system.

- *New:* Downwards rhombic coordinate system.

- *New:* Downwards rhombic rendering shape to accompany the new coordinate
  system.

- *New:* Up- and downwards herringbone rendering shape.

- *Fixed:* The grid align panel now correctly respect or ignores rotation when
  aligning.



------------------------------------------------------------------------


Version 1.6.x
-------------



###Version 1.6.0###

- *New:* Hex-grids can now render in a rhombic shape.



------------------------------------------------------------------------


Version 1.5.x
-------------



###Version 1.5.3###

- Compatibility with Unity 5.



###Version 1.5.2###

- *Fixed:* Changing the `depth` of polar grids affected the cylindric lines
  wrongly.



###Version 1.5.1###

- *Fixed:* Compilation errors when toggling on Playmaker actions.



###Version 1.5.0###

Introducing shearing for rectangular grids.

- *New:* Rectangular grids can now store a `shearing` field to distort them.

- *New:* Custom `Vector6` class for storing the shearing.

- *API change:* The odd herringbone coordinate system has been renamed to
  upwards herringbone. The corresponding methods use the `HerringU` pre- or
  suffix instead of `HerringOdd`; the old methods still work but are marked as
  depracated.

- *API change:* The enumeration `GFAngleMode` has been renamed `AngleMode` and
  moved into the `GridFramework` namespace.

- *API change:* The enumeration `GridPlane` has been moved into the
  `GridFramework` namespace. It is no longer part of the `GFGrid` class.

- *API change:* The class `GFColorVector3` has been renamed `ColorVector3` and
  moved into the `GridFramework.Vectors` namespace.

- *API change:* The class `GFBoolVector3` has been renamed `BoolVector3` and
  moved into the `GridFramework.Vectors` namespace.

- *Enhanced:* Vectrosity methods without parameters can now pick betweem size
  and custom range automatically.

- *Fixed:* Vectrosity methods were broken in previous version.

- Updated the documentation.



------------------------------------------------------------------------


Version 1.4.x
-------------



###Version 1.4.2###

This release is a major overhaul of the rendering and drawing routines and
fixes some issues with coordinate conversion.

- *Fixed:* Wrong rotation when using a rotated grid and an origin offset.

- *Fixed:* Wrong result when convertig coordinates in a hex grid rotated along
  the X- or Y axis.

- *Fixed:* Setting the `relativeSize` flag for polar grids now interprets the
  range properly in grid coordinates.

- *Fixed:* Wrong accessibility for `NearestVertexHO` and `NearestBoxHO` for hex
  grids.

- *New:* Polar grids can now render continuously at any range instead of
  discretely at smoothness steps. 



###Version 1.4.1###

- *Fixed:* compilation error in one of the Playmaker actions (setter and getter
  for depth of layered grids).



###Version 1.4.0###

- Introducing Playmaker support: Almost the entire Grid Framework API can no be
  used as Playmaker actions (some parts of the API are ouside the capabilies of
  Playmaker for now)

- Updated the documentation to include a chapter about Playmaker and how to
  write your own Grid Framework actions.

- *Fixed:* the origin offset resetting every time after exiting play mode.



------------------------------------------------------------------------


Version 1.3.x
-------------



###Version 1.3.8###

- *Fixed:* wrong calculation result in `CubicToWorld` and all related methods
  in hex grids.



###Version 1.3.7###

- *Fixed:* compilation error, sometimes the program might refuse to compile if
  a script used one of the functions NearestVertexW, NearestBoxW or
  NearestBoxW.

- Auto-complete support: Grid Framework's API documentation will now show up in
  MonoDevelop's auto-complete feature. There is no need to jump between editor
  and documentation anymore, it's all integrated.



###Version 1.3.6###

- Changing the origin offset of a grid now takes effect instantly.



###Version 1.3.5###

- Added a new event for when the grid changes in such a way that if would need
  to be redrawn.

- Some of the exmples were broken when Unity updated to version 4.3, now they
  should be working again.

- Overhauled the _undo_ system for the grid align panel to remove the now
  obsolete Unity undo methods.



###Version 1.3.4###

- Added the ability to add a position offset to the grids. This moves the
  origin of a grid by the offset relative to the object's Transform position.
  In the API this is represented by the `originOffset` member of the `GFGrid`
  class.

- Added a chapter about extending Grid Framework without changing the source
  code to the manual. Everything described there are just standard .NET
  features, the chapter is intended for people who were not aware of the
  potential or unfamiliar with it.



###Version 1.3.3###

- Values of GFColorVector3 and GFBoolVector3 were not persistent in version
  1.3.2, fixed this now.

- Examples _Movement with Walls_ and _Sliding Puzzle_ were broken after version
  1.3.2, fixed them now.

- The documentation can now be read online as well. Just delete the offline
  documentation from _WebPlayerTemplates_ and the help menu will notice that
  it's missing and open the web URL.



###Version 1.3.2###

- Hex Grids: new coordinate systems, see the manual page about @ref hex for
  more information.

- New HTML documentation generated with Doxygen replaces the old one.

- Fixed a bug in `Angle2Rotation` when the grid's rotation was not a multiple
  of 90°.

- *New example:* generate a terrain mesh similar to old games like SimCity from
  a plain text file and have it align to a grid.

- *New example:* a rotary phone dial that rotates depending on which number was
  clicked and reports that number back. A great template for disc-shaped GUIs.


Some existing methods have changed in this release, please consult the legacy
support page of the user manual.

- Rect Grids: changed the way `NearestBoxG` works, now there is no offset
  anymore, it returns the actual grid coordinates of the box. Just add `0.5 *
  Vector.one` to the result in your old methods.

- Rect Grids: changed the way `NearestFaceG` works, just like above. Add `0.5 *
  Vector3.one - 0.5 * i` to the result in your old methods (where `i` is the
  index of the plane you used).

- Hex grids: Just like above, nearest vertices of hex grids return their true
  coordinates for whatever coordinate system you choose.


I am sorry for these changes so late , but I realize this differentiation made
things more complicated in the end than they should have been. It's better to
have one unified coordinate system instead. Read the legacy support to learn
how to get the old behaviour back.



###Version 1.3.1###

- Fixed an edge case for `AlignVector3` in rectangular grids.

- in the runtime snapping example you can now click-drag on the grids directly
  and see `AlignVector3` in action (turn on gizmos in game view to see).

- added the _PointDebug_ script to the above example for that purpose.

- Changed the way movement is done in the grid-based movement example, now the
  sphere will always take the straight path.



###Version 1.3.0###

Introducing polar grids to Grid Framework: comes with all the usual methods and two coordinate systems.

- Added `up`, `right` and `forward` members to rectangular grids.

- Added `sides`, `width` and `height` members to hex grids.

- Added the enum `GFAngleMode {radians, degree}` to specify an angle type;
  currently only used in methods of polar grids.

- Added the enum `HexDirection` for cardinal directions (north, north-east,
  east, ...) in hex grids.

- Added the `GetDirection` method to hex grids to convert a cardinal direction
  to a world space direction vector.

- Added a lot of minor conversion methods for rotation, angles, sectors and so
  on in hex grids

- Hex grids and polar grids now both inherit from `GFLayeredGrid`, which in
  return inherits from `GFGrid`.

- The Lights Off example now features a polar grid as well.

- Procedural mesh generation for grid faces in the Lights Off example.

- Mouse handling in runtime snapping example changed because it was confusing a
  lot of users who just copy-pasted the code.



------------------------------------------------------------------------


Version 1.2.x
-------------


###Version 1.2.5###

This release serves as a preparation for Version 1.3.0, which will add polar
grids

- the methods 'NearestVertex/Face/BoxW' and 'NearestVertex/Face/BoxG' replace
  'FindNearestVertex/Face/Box' and 'FindNearestVertex/Face/Box' respectively.


This is just a change in name, as the old nomenclature was confusing and makes
no sense for grids with multiple coordinate systems, but the syntax stays the
same.

The old methods will throw compiler warnings but will still work fine. You can
run a Search&Replace through your scripts to get rid of them.

- The 'GFBoolVector3' class can now be instantiated via 'GFBoolVector3.True'
  and 'GFBoolVector3.False' to create an all-_true_ or all-_false_ vector.

- Similarly you can use `GFColorVector3.RGB`, `GFColorVector3.CMY` and
  `GFColorVector3.BGW` for half-transparent standard colour vectors

- Various code cleanup.



###Version 1.2.4###

- Performance improvement by caching draw points. As long as the grid hasn't
  been changed the method CalculateDrawPoints will reuse the existing points
  instead of calculating them again.

- Added explanation about rendering performance to the user manual. It explains
  what exactly happens, what lowers performance and what techniques can improve
  performance.

- *New exmple:* A seemingly endless grid scrolls forever. This is achieved by
  adjusting the rendering range dynamically and we add a little buffer to make
  use of the new caching feature.



###Version 1.2.3###

- Added the ability to use a separate set of colours for rendering and drawing.

- Added the ability to have the size of drawings/renderings be relative to the
  spacing of the grid instead of absolute in world coordinates.

- Some examples were broken after the last update after adding accessors to the
  code, fixed now.



###Version 1.2.2###

- Fixed a typo that could prevent a finished project from building correctly.

- _New example:_ a sliding block puzzle working entirely without physics.

- Removed the variables `minimumSpacing` and `minimumRadius` from `GFRectGrid`
  and `GFHexGrid`. Instead they both use accessors that limit spacing and
  radius to 0.1.

- The members `size`, `renderTo` and `renderFrom` are now using accessors as
  well, this prevents setting them to nonsensical values.

- Removed the redundant _Use Custom Rendering Range_ flag in the inspector
  (doesn't change anything in the API though, it's just cosmetic)

- The foldout state for _Draw & Render Settings_ in the inspector should stick
  now (individual for both grid types).

- Several minor tweaks under the hood.



###Version 1.2.1###

- Updated the Lights Off example to use hex grids.



###Version 1.2.0###

Introducing hexagonal grids: use hexagons instead of rectangles for your grids.
Comes with all the methods you've come to know from rectangular grids and uses
a herringbone pattern as the coordinate system.

- The movement example scripts now take a 'GFGrid' instead of a 'GFRectGrid',
  allowing the user to use both rectangular and hexagonal grids without
  changing the code.



------------------------------------------------------------------------


Version 1.1.x
-------------


###Version 1.1.10###

- _New method:_ 'ScaleVector3' lets you scale a `Vector3` to the grid instead
  of a `Transform`.



###Version 1.1.9###

- _New method:_ `AlignVector3` lets you align a single point represented as a
  `Vector3` instead of a `Transform` to a grid.

- Added the ability to lock a certain axes when calling `AlignTransform` and
  `AlignVector3`.

- Added a new constructor to both `GFBoolVector3` and `GFColorVector3` that
  lets you pass one parameter that gets applied to all components.

- You can now lock axes in the Grid Align Panel as well.

- Aligning objects via the Grid Align Panel which already are in place won't do
  anything, meaning they won't create redundant Undo entries anymore.

- Fixed an issue in `GetVectrosityPointsSeperate`.

- Renamed the classes `BoolVector3` and `ColorVector3` to `GFBoolVector3` and
  `GFColorVector3` to avoid name collision.

- The member `size` has always been a member of `GFGrid`, not `GFRectGrid`, I
  fixed that mistake in the documentation.

- Minor code cleanup and removing redundant code.



###Version 1.1.8###

- Previously if you unloaded a level with a grid that was rendered the game
  could have crashed. Fixed that issue.



###Version 1.1.7###

- Fixed a typo that prevented adding the `GFGridRenderCamera` component from
  the menu bar.

- _New example:_ design your levels in a plain text file and use Grid Framework
  and a text parser to build them during runtime. No need to change scenes when
  switching levels, faster than placing blocks by hand and great for user-made
  mods.

- _New example:_ a continuation of the grid-based movement example where you
  can place obstacles on the grid and the sphere will avoid them. Works without
  using any physics or colliders. 



###Version 1.1.6###

*Important:* The classes `Grid` and `RectGrid` have been renamed to `GFGrid`
and `GFRectGrid`. This was done to prevent name collision with classes users
might already be using or classes from other extensions. I apologize for the
inconvenience.

- Minor code cleanup and performance increase in `GFRectGrid`.



###Version 1.1.5###

- Custom rendering range affects now drawing as well.



###Version 1.1.4###

- Fixed an issue where lines with width would be rendered on top of objects
  even though they should be underneath.



###Version 1.1.3###

- Support for custom range for rendering instead of the grid's `size`.

- From now on all files should install in the right place on their own, no more
  moving scripts manually.



###Version 1.1.2###

- Integration into the menu bar.

- Vectrosity support.

- Documentation split into a separate user manual and a scripting reference.



###Version 1.1.1###

- Line width for rendering now possible.



###Version 1.1.0###

- Introducing grid rendering.

- New inspector panel for `RectGrid`.



------------------------------------------------------------------------


Version 1.0.x
-------------



###Version 1.0.1###

- Fixed rotation for cube shaped debug gizmos.



###Version 1.0.0###

- Initial release.
