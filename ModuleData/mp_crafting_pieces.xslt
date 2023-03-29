<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
    <xsl:output omit-xml-declaration="yes"/>
    <xsl:template match="@*|node()">
        <xsl:copy>
            <xsl:apply-templates select="@*|node()"/>
        </xsl:copy>
    </xsl:template>
  <xsl:template match="CraftingPiece[@id='mp_empire_2haxe_head']"/>
  <xsl:template match="CraftingPiece[@id='mp_vlandian_throwing_axe_head_extra']"/>
  <xsl:template match="CraftingPiece[@id='mp_battanian_throwing_axe_head']"/>
  <xsl:template match="CraftingPiece[@id='mp_empire_axe_head']"/>
  <xsl:template match="CraftingPiece[@id='mp_empire_battle_axe_head']"/>
  <xsl:template match="CraftingPiece[@id='mp_vlandia_bastard_axe_head']"/>
  <xsl:template match="CraftingPiece[@id='mp_khuzait_glaive_blade']"/>
  <xsl:template match="CraftingPiece[@id='mp_vlandian_mace_head']"/>
  <xsl:template match="CraftingPiece[@id='mp_aserai_maul_head']"/>
  <xsl:template match="CraftingPiece[@id='mp_empire_heavy_mace_head']"/>
  <xsl:template match="CraftingPiece[@id='mp_khuzait_mace_head']"/>
  <xsl:template match="CraftingPiece[@id='mp_khuzait_heavy_mace_head']"/>
  <xsl:template match="CraftingPiece[@id='mp_aserai_mace_head']"/>
  <xsl:template match="CraftingPiece[@id='mp_aserai_heavy_mace_head']"/>
  <xsl:template match="CraftingPiece[@id='mp_sturgia_mace_head']"/>
  <xsl:template match="CraftingPiece[@id='mp_battania_mace_head']"/>
  <xsl:template match="CraftingPiece[@id='mp_sturgia_2hhammer_head']"/>
  <xsl:template match="CraftingPiece[@id='mp_battania_2hhammer_head']"/>
</xsl:stylesheet>