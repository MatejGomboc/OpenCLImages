/*
	Copyright (C) 2019 Matej Gomboc

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU Affero General Public License as published
    by the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU Affero General Public License for more details.

    You should have received a copy of the GNU Affero General Public License
    along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

constant sampler_t sampler = CLK_NORMALIZED_COORDS_FALSE | CLK_ADDRESS_CLAMP_TO_EDGE | CLK_FILTER_NEAREST;

kernel void Test(read_only image2d_t input_image, write_only image2d_t output_image)
{
	float4 input_pixel = read_imagef(input_image, sampler, (int2)(get_global_id(0), get_global_id(1)));

	write_imagef(output_image, (int2)(get_global_id(0), get_global_id(1)), (float4)(
		input_pixel.z, // R
		input_pixel.x, // G
		input_pixel.y, // B
		1.0f  // A
	));
}
